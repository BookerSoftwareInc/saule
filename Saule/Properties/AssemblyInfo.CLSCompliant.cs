// CLS compliance was dropped entirely along with the rest of the legacy AssemblyInfo.cs during the
// SDK-style conversion, matching Booker.ApplicationInsights/Booker.Core.Infrastructure.Libraries.WebApi's
// convention of not declaring it at all. Restored per PR review (2026-09-09): net47/netstandard2.0
// genuinely are still CLS-compliant, and losing that assertion for them was an unnecessary regression
// from the original library's posture, not something forced by anything in this migration. net10.0
// still can't claim it - the Http/NetCore/* classes necessarily expose ASP.NET Core MVC types
// (ActionFilterAttribute, ActionExecutingContext, IMvcBuilder, etc.) on their public surface, and
// those types are themselves not CLS-compliant (confirmed by an actual CS3001/CS3002/CS3009 build
// failure earlier this session when this was first attempted unconditionally).
#if !NET10_0
[assembly: System.CLSCompliant(true)]
#endif
