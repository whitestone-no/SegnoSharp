using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Whitestone.SegnoSharp.Shared.Attributes.Controllers;
using Whitestone.SegnoSharp.Shared.Models.Security;

namespace Whitestone.SegnoSharp.Shared.Abstractions;

[ApiController]
[ApiEndpoint]
[Authorize(AuthenticationSchemes = AuthenticationSchemes.All)]
public abstract class ApiControllerBase : ControllerBase;