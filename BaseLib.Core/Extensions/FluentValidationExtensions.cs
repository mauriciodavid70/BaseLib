using System;

namespace FluentValidation
{
    /// <summary>Extension methods that integrate <c>BaseLib.Core</c> reason codes with FluentValidation rules.</summary>
    public static class FluentValidationExtensions
    {
        /// <summary>
        /// Sets the FluentValidation error code and message from a domain enum value decorated with
        /// <see cref="System.ComponentModel.DescriptionAttribute"/>.
        /// The numeric enum value becomes the error code and the description becomes the error message.
        /// </summary>
        public static IRuleBuilderOptions<T, TProperty> WithReasonCode<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, Enum reasonCode, params string[] messages)
        {
            DefaultValidatorOptions.Configurable(rule).Current.ErrorCode = Convert.ToInt32(reasonCode).ToString();
            DefaultValidatorOptions.Configurable(rule).Current.SetErrorMessage(reasonCode?.GetDescription());
            return rule;
        }
    }
}