using System.Text;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Essentials.Web.Interop;

namespace Microsoft.Maui.Essentials.Web.Communication;

/// <summary>Composes through the user's mailto: handler.</summary>
public class WebEmail : IEmail
{
	public bool IsComposeSupported => true;

	public async Task ComposeAsync(EmailMessage? message)
	{
		if (message?.Attachments is { Count: > 0 })
			throw new FeatureNotSupportedException("mailto: links cannot carry attachments.");

		var query = new List<string>();
		Add(query, "cc", message?.Cc);
		Add(query, "bcc", message?.Bcc);
		if (!string.IsNullOrEmpty(message?.Subject))
			query.Add("subject=" + Uri.EscapeDataString(message.Subject));
		if (!string.IsNullOrEmpty(message?.Body))
			query.Add("body=" + Uri.EscapeDataString(message.Body));

		var to = string.Join(',', message?.To?.Select(Uri.EscapeDataString) ?? []);
		await EssentialsJs.EnsureLoadedAsync();
		EssentialsJs.Navigate($"mailto:{to}{(query.Count > 0 ? "?" + string.Join('&', query) : "")}");
	}

	static void Add(List<string> query, string name, List<string>? recipients)
	{
		if (recipients is { Count: > 0 })
			query.Add(name + "=" + string.Join(',', recipients.Select(Uri.EscapeDataString)));
	}
}

/// <summary>Dials through tel:, which desktop browsers hand to a calling app if one is registered.</summary>
public class WebPhoneDialer : IPhoneDialer
{
	public bool IsSupported => true;

	public void Open(string number)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(number);
		var digits = new string(number.Where(c => char.IsDigit(c) || c is '+' or '*' or '#').ToArray());
		EssentialsJs.WhenLoaded(() => EssentialsJs.Navigate("tel:" + Uri.EscapeDataString(digits)));
	}
}

public class WebSms : ISms
{
	public bool IsComposeSupported => true;

	public async Task ComposeAsync(SmsMessage? message)
	{
		var sb = new StringBuilder("sms:");
		if (message?.Recipients is { Count: > 0 } recipients)
			sb.Append(string.Join(',', recipients.Select(Uri.EscapeDataString)));
		if (!string.IsNullOrEmpty(message?.Body))
			sb.Append("?body=").Append(Uri.EscapeDataString(message.Body));
		await EssentialsJs.EnsureLoadedAsync();
		EssentialsJs.Navigate(sb.ToString());
	}
}

/// <summary>
/// Contact Picker API (Chrome on Android). The browser never exposes the whole address book,
/// so GetAllAsync is unsupported.
/// </summary>
public class WebContacts : IContacts
{
	public async Task<Contact?> PickContactAsync()
	{
		await EssentialsJs.EnsureLoadedAsync();
		if (!EssentialsJs.ContactsSupported())
			throw new FeatureNotSupportedException("This browser does not support the Contact Picker API.");

		var record = await EssentialsJs.PickContact();
		if (record is null)
			return null;

		var parts = record.Split('\u001f');
		var name = parts.ElementAtOrDefault(0) ?? string.Empty;
		var emails = Lines(parts.ElementAtOrDefault(1)).Select(e => new ContactEmail(e)).ToList();
		var phones = Lines(parts.ElementAtOrDefault(2)).Select(p => new ContactPhone(p)).ToList();
		return new Contact(id: string.Empty, namePrefix: null, givenName: name, middleName: null, familyName: null,
			nameSuffix: null, phones: phones, email: emails, displayName: name);
	}

	public Task<IEnumerable<Contact>> GetAllAsync(CancellationToken cancellationToken = default)
		=> throw new FeatureNotSupportedException("Browsers only allow picking individual contacts.");

	static IEnumerable<string> Lines(string? value)
		=> string.IsNullOrEmpty(value) ? [] : value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
}
