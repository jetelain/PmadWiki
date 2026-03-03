namespace Pmad.Wiki.Models;

/// <summary>
/// Defines the type of a template parameter.
/// </summary>
public enum WikiTemplateParameterType
{
    /// <summary>
    /// A text input field.
    /// </summary>
    Text,
    
    /// <summary>
    /// A numeric input field.
    /// </summary>
    Number,
    
    /// <summary>
    /// A date input field.
    /// </summary>
    Date,
    
    /// <summary>
    /// A datetime input field.
    /// </summary>
    DateTime
}
