using Shiny.Maui.Controls;

namespace Sample.Features.PasswordStrength;

/// <summary>
/// Stands in for the real reason <see cref="IPasswordStrengthEvaluator"/> is asynchronous — a Have I
/// Been Pwned range query or a policy endpoint. It defers to the built-in heuristic after a delay,
/// so the demo shows the debounce and the cancellation actually working.
/// </summary>
public class DelayedPasswordStrengthEvaluator : IPasswordStrengthEvaluator
{
    public async ValueTask<PasswordStrengthResult> EvaluateAsync(
        PasswordStrengthRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await Task.Delay(400, cancellationToken);
        return await DefaultPasswordStrengthEvaluator.Instance.EvaluateAsync(request, cancellationToken);
    }
}
