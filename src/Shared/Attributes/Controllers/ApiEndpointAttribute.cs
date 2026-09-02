using System;
using Whitestone.SegnoSharp.Shared.Interfaces;

namespace Whitestone.SegnoSharp.Shared.Attributes.Controllers;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ApiEndpointAttribute : Attribute, IApiEndpointMetadata;