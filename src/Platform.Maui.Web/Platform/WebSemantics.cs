using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform.Web.Handlers;

namespace Microsoft.Maui.Platform.Web;

/// <summary>
/// Implemented by handlers whose focusable element is not their root, such as a switch's checkbox
/// inside its label. Names and hints go on this element so assistive technology reads them when it
/// lands there.
/// </summary>
public interface IWebSemanticTarget
{
	WebElement SemanticElement { get; }
}

/// <summary>
/// Translates MAUI's <see cref="SemanticProperties"/> and <see cref="AutomationProperties"/> into ARIA.
/// </summary>
public static class WebSemantics
{
	sealed class State
	{
		public string? BaseRole;
		public VisualElement? PendingLabeledBy;
	}

	static readonly ConditionalWeakTable<WebElement, State> States = new();
	static int _nextId;

	/// <summary>
	/// The accessible name MAUI asks for: SemanticProperties.Description, then the obsolete
	/// AutomationProperties.Name, then a Picker's Title.
	/// </summary>
	public static string? GetName(IView view)
	{
#pragma warning disable CS0618 // AutomationProperties.Name is obsolete but still honored on the native platforms.
		var name = NonEmpty(view.Semantics?.Description)
			?? (view is BindableObject bindable ? NonEmpty(AutomationProperties.GetName(bindable)) : null);
#pragma warning restore CS0618
		return name ?? (view is IPicker picker ? NonEmpty(picker.Title) : null);
	}

	/// <summary>
	/// Mapped for the AutomationProperties keys. Re-maps Semantics through the handler, so a handler
	/// that overrides the Semantics mapping (Label) also sees these changes.
	/// </summary>
	public static void MapAutomationProperty(IViewHandler handler, IView view)
	{
		var root = (handler as IWebElementHandler)?.Element;
		if (root is not null && (UsesAutomationProperties(view) || States.TryGetValue(root, out _)))
			handler.UpdateValue(nameof(IView.Semantics));
	}

	public static void Apply(IViewHandler handler, WebElement root, IView view)
	{
		var bindable = view as BindableObject;
		var semantics = view.Semantics;

		// Most views set nothing; leave their DOM alone until one of them does.
		if (!States.TryGetValue(root, out var state))
		{
			if (semantics is null && !UsesAutomationProperties(view) && view is not IPicker { Title.Length: > 0 })
				return;
			// Handlers give some roots a role of their own (list, switch, progressbar); a heading or a
			// presentational role temporarily replaces it, so remember it to put back.
			state = States.GetValue(root, el => new State { BaseRole = el.GetAttribute("role") });
		}

#pragma warning disable CS0618 // HelpText and LabeledBy are obsolete but still honored on the native platforms.
		var hint = NonEmpty(semantics?.Hint) ?? (bindable is null ? null : NonEmpty(AutomationProperties.GetHelpText(bindable)));
		var labeledBy = bindable is null ? null : AutomationProperties.GetLabeledBy(bindable);
#pragma warning restore CS0618
		var name = GetName(view);
		var inTree = bindable is null ? null : AutomationProperties.GetIsInAccessibleTree(bindable);
		var excluded = bindable is null ? null : AutomationProperties.GetExcludedWithChildren(bindable);
		var heading = semantics?.HeadingLevel ?? SemanticHeadingLevel.None;

		var tag = root.GetString("tagName")?.ToLowerInvariant();
		var generic = tag is "div" or "span" or "canvas";
		var container = generic && view is ILayout or IContentView or IScrollView;

		// IsInAccessibleTree=false hides the element itself. A container's children stay reachable, as
		// they do natively, so it loses its own semantics instead of hiding its whole subtree.
		var presentational = inTree == false && container;
		var hidden = excluded == true || (inTree == false && !container);
		root.SetAttribute("aria-hidden", hidden ? "true" : null);

		var role = state.BaseRole;
		if (presentational)
			role = "none";
		else if (heading != SemanticHeadingLevel.None)
			role = "heading";
		else if (role is null && name is not null && generic)
			// aria-label is ignored on an element with no role, so give described views one.
			role = container ? "group" : view is IGraphicsView or IShapeView ? "img" : null;
		root.SetAttribute("role", role);
		root.SetAttribute("aria-level", role == "heading" ? ((int)heading).ToString(CultureInfo.InvariantCulture) : null);

		if (presentational)
		{
			name = null;
			hint = null;
			labeledBy = null;
		}

		var target = (handler as IWebSemanticTarget)?.SemanticElement ?? root;
		if (tag == "img")
			// An empty alt marks an undescribed image as decorative, as MAUI images are by default.
			root.SetAttribute("alt", name ?? string.Empty);
		else
			target.SetAttribute("aria-label", name);
		target.SetAttribute("aria-description", hint);
		target.SetAttribute("aria-labelledby", IdOf(labeledBy, view, state));
	}

	static string? IdOf(VisualElement? label, IView owner, State state)
	{
		if (label is null)
			return null;

		if ((label.Handler as IWebElementHandler)?.Element is not { } element)
		{
			// The label hasn't been rendered yet; map again once it has.
			if (!ReferenceEquals(state.PendingLabeledBy, label))
			{
				state.PendingLabeledBy = label;
				void OnHandlerChanged(object? sender, EventArgs e)
				{
					label.HandlerChanged -= OnHandlerChanged;
					state.PendingLabeledBy = null;
					owner.Handler?.UpdateValue(nameof(IView.Semantics));
				}
				label.HandlerChanged += OnHandlerChanged;
			}
			return null;
		}

		var id = element.GetAttribute("id");
		if (string.IsNullOrEmpty(id))
		{
			id = "maui-" + (++_nextId).ToString(CultureInfo.InvariantCulture);
			element.SetAttribute("id", id);
		}
		return id;
	}

	static bool UsesAutomationProperties(IView view)
	{
		if (view is not BindableObject bindable)
			return false;
#pragma warning disable CS0618
		return bindable.IsSet(AutomationProperties.IsInAccessibleTreeProperty)
			|| bindable.IsSet(AutomationProperties.ExcludedWithChildrenProperty)
			|| bindable.IsSet(AutomationProperties.NameProperty)
			|| bindable.IsSet(AutomationProperties.HelpTextProperty)
			|| bindable.IsSet(AutomationProperties.LabeledByProperty);
#pragma warning restore CS0618
	}

	static string? NonEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
