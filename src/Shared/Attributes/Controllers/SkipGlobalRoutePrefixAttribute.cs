using System;

namespace Whitestone.SegnoSharp.Shared.Attributes.Controllers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SkipGlobalRoutePrefixAttribute : Attribute;