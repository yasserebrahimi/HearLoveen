using System;
using System.Collections.Generic;
using HearLoveen.Infrastructure;

namespace HearLoveen.Tests.Testing;
public class TestCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; set; } = true;
    public bool IsAdmin { get; set; }
    public bool IsTherapist { get; set; }
    public Guid? UserId { get; set; } = Guid.NewGuid();
    public IReadOnlyCollection<Guid> ChildScope { get; set; } = Array.Empty<Guid>();
}
