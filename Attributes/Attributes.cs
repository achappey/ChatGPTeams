// Custom attribute to describe a method
using System;

namespace achappey.ChatGPTeams.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class MethodDescriptionAttribute : Attribute
{
    public string Description { get; }

    public MethodDescriptionAttribute(string description)
    {
        Description = description;
    }
}

// Custom attribute to describe a parameter
[AttributeUsage(AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
public sealed class ParameterDescriptionAttribute : Attribute
{
    public string Description { get; }

    public ParameterDescriptionAttribute(string description)
    {
        Description = description;
    }
}
