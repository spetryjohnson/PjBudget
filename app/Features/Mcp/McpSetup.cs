namespace PjBudget.Features.Mcp;

/*
 * MCP server scaffolding
 * ----------------------
 * The ModelContextProtocol NuGet packages are referenced in the .csproj, but no MCP server is
 * registered or mapped in Program.cs. This keeps the starter clean while leaving the dependencies
 * in place so you can light up an MCP endpoint without hunting for packages.
 *
 * To enable a basic (no-auth) MCP server, add the following to Program.cs:
 *
 *     using ModelContextProtocol.Server;
 *
 *     builder.Services
 *         .AddMcpServer()
 *         .WithHttpTransport(options => options.Stateless = true)
 *         .WithToolsFromAssembly();
 *
 *     // ...after app.UseAuthentication() / app.UseAuthorization():
 *     app.MapMcp("/mcp");
 *
 * Then create a tool class like:
 *
 *     [McpServerToolType]
 *     public sealed class PingTool
 *     {
 *         [McpServerTool, Description("Replies with pong")]
 *         public static string Ping() => "pong";
 *     }
 *
 * For OAuth-protected MCP (Claude Connectors) and/or static bearer token auth, see the
 * RascalTimesheetReview reference implementation in Features/McpConnector/.
 */
internal static class McpSetup
{
}
