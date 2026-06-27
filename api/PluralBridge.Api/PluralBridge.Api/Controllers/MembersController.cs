using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace PluralBridge.Api.Controllers;

/// <summary>
/// Provides protected member endpoints for the Phase 3 proof surface.
/// Members are returned for a specific imported PluralBridge system.
/// </summary>
[ApiController]
[Route(Globals.membersRoute)]
public sealed class MembersController(
	IConfiguration configuration,
	ILogger<MembersController> logger) : ControllerBase
{
	/// <summary>
	/// Returns all members for the requested system from the validated proof database.
	/// The response includes count metadata and keeps write capability explicitly disabled.
	/// </summary>
	/// <param name="systemId">The PluralBridge system identifier used to scope the member query.</param>
	/// <returns>
	/// HTTP 200 with member rows, total count, and read-only capability metadata.
	/// </returns>
	[HttpGet]
	public async Task<IActionResult> Get(Guid systemId)
	{
		var requestTrace = RequestTraceContext.Create(
			HttpContext.TraceIdentifier,
			HttpContext.Request.Headers.TryGetValue(Globals.correlationID, out var correlationId)
				? correlationId.ToString()
				: null);

		var connectionString = configuration.GetConnectionString(Globals.connectionString);

		if (string.IsNullOrWhiteSpace(connectionString))
		{
			requestTrace.LogStage(
				logger,
				nameof(LogStageParts.error_path),
				nameof(LogStageParts.reached));

			return Problem(
				title: Globals.missingConnectionString,
				detail: Globals.missingConnStringDetail,
				statusCode: StatusCodes.Status500InternalServerError);
		}

		try
		{
			await using var connection = new SqlConnection(connectionString);
			await connection.OpenAsync();

			var accessContext = await AccessContextHelper.ResolveCurrentAccessAsync(
				connection,
				requestTrace,
				logger);

			if (accessContext is null)
			{
				requestTrace.LogStage(
					logger,
					nameof(LogStageParts.error_path),
					nameof(LogStageParts.reached));

				return Unauthorized(new
				{
					api = Globals.apiName,
					phase = Globals.projectPhase,
					endpoint = $"{Globals.systemsEndpointRoot}/{systemId}/{Globals.membersEndpointSegment}",
					canWrite = false,
					systemId,
					error = Globals.cantResolveAccess
				});
			}

			if (!AccessContextHelper.IsAuthorizedForCurrentSystem(accessContext) || accessContext.CurrentSystem.SystemId != systemId)
			{
				requestTrace.LogStage(
					logger,
					nameof(LogStageParts.error_path),
					nameof(LogStageParts.reached));

				return Forbid();
			}

			var dataAccessStopwatch = Stopwatch.StartNew();

			requestTrace.LogStage(
				logger,
				nameof(LogStageParts.data_access),
				nameof(LogStageParts.started));

			List<Member>? members = null;
			var count = 0;

			try
			{
				members = await ReadMembersAsync(
					connection,
					accessContext.CurrentSystem.SystemId);
				count = members.Count;

				dataAccessStopwatch.Stop();

				requestTrace.LogStage(
					logger,
					nameof(LogStageParts.data_access),
					nameof(LogStageParts.completed),
					dataAccessStopwatch.Elapsed);
			}
			catch
			{
				dataAccessStopwatch.Stop();

				requestTrace.LogStage(
					logger,
					nameof(LogStageParts.data_access),
					nameof(LogStageParts.failed),
					dataAccessStopwatch.Elapsed);

				requestTrace.LogStage(
					logger,
					nameof(LogStageParts.error_path),
					nameof(LogStageParts.reached));

				throw;
			}

			return Ok(new
			{
				api = Globals.apiName,
				phase = Globals.projectPhase,
				endpoint = $"{Globals.systemsEndpointRoot}/{systemId}/{Globals.membersEndpointSegment}",
				canWrite = false,
				systemId = accessContext.CurrentSystem.SystemId,
				count = count,
				members
			});
		}
		catch
		{
			requestTrace.LogStage(
				logger,
				nameof(LogStageParts.error_path),
				nameof(LogStageParts.reached));

			return Problem(
				title: Globals.requestFailed,
				detail: Globals.currConfiguredAccount,
				statusCode: StatusCodes.Status500InternalServerError);
		}
	}

	/// <summary>
	/// Returns one member for the requested system from the validated proof database.
	/// The response includes read-only capability metadata for the selected member.
	/// </summary>
	/// <param name="systemId">The PluralBridge system identifier used to scope the member query.</param>
	/// <param name="memberId">The PluralBridge member identifier.</param>
	/// <returns>
	/// HTTP 200 with the selected member and read-only capability metadata.
	/// </returns>
	[HttpGet(Globals.routeMemberId)]
	public async Task<IActionResult> Get(
		Guid systemId,
		Guid memberId)
	{
		var requestTrace = RequestTraceContext.Create(
			HttpContext.TraceIdentifier,
			HttpContext.Request.Headers.TryGetValue(Globals.correlationID, out var correlationId)
				? correlationId.ToString()
				: null);

		try
		{
			var connectionString = configuration.GetConnectionString(Globals.connectionString);

			if (string.IsNullOrWhiteSpace(connectionString))
			{
				requestTrace.LogStage(
					logger,
					nameof(LogStageParts.error_path),
					nameof(LogStageParts.reached));

				return Problem(
					title: Globals.missingConnectionString,
					detail: Globals.missingConnStringDetail,
					statusCode: StatusCodes.Status500InternalServerError);
			}

			await using var connection = new SqlConnection(connectionString);
			await connection.OpenAsync();

			var accessContext = await AccessContextHelper.ResolveCurrentAccessAsync(connection);

			if (accessContext is null)
			{
				requestTrace.LogStage(
					logger,
					nameof(LogStageParts.error_path),
					nameof(LogStageParts.reached));

				return Unauthorized(new
				{
					api = Globals.apiName,
					phase = Globals.projectPhase,
					endpoint = $"{Globals.systemsEndpointRoot}/{systemId}/{Globals.membersEndpointSegment}/{memberId}",
					canWrite = false,
					systemId,
					memberId,
					error = Globals.cantResolveAccess
				});
			}

			if (!AccessContextHelper.IsAuthorizedForCurrentSystem(accessContext) || accessContext.CurrentSystem.SystemId != systemId)
			{
				requestTrace.LogStage(
					logger,
					nameof(LogStageParts.error_path),
					nameof(LogStageParts.reached));

				return Forbid();
			}

			var dataAccessStopwatch = Stopwatch.StartNew();

			requestTrace.LogStage(
				logger,
				nameof(LogStageParts.data_access),
				nameof(LogStageParts.started));

			Member? member;

			try
			{
				member = await ReadMemberAsync(
					connection,
					accessContext.CurrentSystem.SystemId,
					memberId);

				if (member is null)
				{
					return NotFound(new
					{
						api = Globals.apiName,
						phase = Globals.projectPhase,
						endpoint = $"{Globals.systemsEndpointRoot}/{systemId}/{Globals.membersEndpointSegment}/{memberId}",
						canWrite = false,
						systemId = accessContext.CurrentSystem.SystemId,
						memberId
					});
				}
				dataAccessStopwatch.Stop();

				requestTrace.LogStage(
					logger,
					nameof(LogStageParts.data_access),
					nameof(LogStageParts.completed),
					dataAccessStopwatch.Elapsed);
			}
			catch
			{
				dataAccessStopwatch.Stop();

				requestTrace.LogStage(
					logger,
					nameof(LogStageParts.data_access),
					nameof(LogStageParts.failed),
					dataAccessStopwatch.Elapsed);

				requestTrace.LogStage(
					logger,
					nameof(LogStageParts.error_path),
					nameof(LogStageParts.reached));

				throw;
			}

			return Ok(new
			{
				api = Globals.apiName,
				phase = Globals.projectPhase,
				endpoint = $"{Globals.systemsEndpointRoot}/{systemId}/{Globals.membersEndpointSegment}/{memberId}",
				canWrite = false,
				systemId = accessContext.CurrentSystem.SystemId,
				memberId,
				member
			});
		}
		catch
		{
			requestTrace.LogStage(
				logger,
				nameof(LogStageParts.error_path),
				nameof(LogStageParts.reached));

			return Problem(
				title: Globals.requestFailed,
				detail: Globals.currConfiguredAccount,
				statusCode: StatusCodes.Status500InternalServerError);
		}
	}

	/// <summary>
	/// Reads member rows for one system from the validated proof database.
	/// The selected fields expose imported member metadata without exposing raw source payloads.
	/// </summary>
	/// <param name="connection">An open SQL Server connection to the PluralBridge proof database.</param>
	/// <param name="systemId">The PluralBridge system identifier used to scope the member query.</param>
	/// <returns>The member rows ordered by display name and member identifier.</returns>
	private static async Task<List<Member>> ReadMembersAsync(SqlConnection connection, Guid systemId)
	{
		const string sql = """
		                   SELECT
		                       MemberId,
		                       SystemId,
		                       DisplayName,
		                       Pronouns,
		                       Description,
		                       Color,
		                       IsArchived,
		                       ArchivedReason,
		                       IsPrivate,
		                       PreventTrusted,
		                       PreventsFrontNotifications,
		                       ReceiveMessageBoardNotifications,
		                       SupportsDescriptionMarkdown,
		                       LastOperationTimeMs,
		                       ImportedAtUtc,
		                       CreatedAtUtc,
		                       UpdatedAtUtc
		                   FROM dbo.pb_members
		                   WHERE SystemId = @SystemId
		                   ORDER BY DisplayName, MemberId;
		                   """;

		var members = new List<Member>();

		await using var command = new SqlCommand(sql, connection);
		command.Parameters.AddWithValue("@SystemId", systemId);

		await using var reader = await command.ExecuteReaderAsync();

		while (await reader.ReadAsync())
		{
			members.Add(new Member(
				reader.GetGuid(0),
				reader.GetGuid(1),
				reader.GetString(2),
				reader.IsDBNull(3) ? null : reader.GetString(3),
				reader.IsDBNull(4) ? null : reader.GetString(4),
				reader.IsDBNull(5) ? null : reader.GetString(5),
				reader.IsDBNull(6) ? null : reader.GetBoolean(6),
				reader.IsDBNull(7) ? null : reader.GetString(7),
				reader.IsDBNull(8) ? null : reader.GetBoolean(8),
				reader.IsDBNull(9) ? null : reader.GetBoolean(9),
				reader.IsDBNull(10) ? null : reader.GetBoolean(10),
				reader.IsDBNull(11) ? null : reader.GetBoolean(11),
				reader.IsDBNull(12) ? null : reader.GetBoolean(12),
				reader.IsDBNull(13) ? null : reader.GetInt64(13),
				reader.GetDateTime(14),
				reader.GetDateTime(15),
				reader.IsDBNull(16) ? null : reader.GetDateTime(16)));
		}

		return members;
	}

	/// <summary>
	/// Reads one member row for one system from the validated proof database.
	/// The selected fields expose imported member metadata without exposing raw source payloads.
	/// </summary>
	/// <param name="connection">An open SQL Server connection to the PluralBridge proof database.</param>
	/// <param name="systemId">The PluralBridge system identifier used to scope the member query.</param>
	/// <param name="memberId">The PluralBridge member identifier.</param>
	/// <returns>The selected member row, or null when no matching row exists.</returns>
	private static async Task<Member?> ReadMemberAsync(
		SqlConnection connection,
		Guid systemId,
		Guid memberId)
	{
		const string sql = """
	                   SELECT
	                       MemberId,
	                       SystemId,
	                       DisplayName,
	                       Pronouns,
	                       Description,
	                       Color,
	                       IsArchived,
	                       ArchivedReason,
	                       IsPrivate,
	                       PreventTrusted,
	                       PreventsFrontNotifications,
	                       ReceiveMessageBoardNotifications,
	                       SupportsDescriptionMarkdown,
	                       LastOperationTimeMs,
	                       ImportedAtUtc,
	                       CreatedAtUtc,
	                       UpdatedAtUtc
	                   FROM dbo.pb_members
	                   WHERE SystemId = @SystemId
	                     AND MemberId = @MemberId;
	                   """;

		await using var command = new SqlCommand(sql, connection);
		command.Parameters.AddWithValue("@SystemId", systemId);
		command.Parameters.AddWithValue("@MemberId", memberId);

		await using var reader = await command.ExecuteReaderAsync();

		if (!await reader.ReadAsync())
		{
			return null;
		}

		return new Member(
			reader.GetGuid(0),
			reader.GetGuid(1),
			reader.GetString(2),
			reader.IsDBNull(3) ? null : reader.GetString(3),
			reader.IsDBNull(4) ? null : reader.GetString(4),
			reader.IsDBNull(5) ? null : reader.GetString(5),
			reader.IsDBNull(6) ? null : reader.GetBoolean(6),
			reader.IsDBNull(7) ? null : reader.GetString(7),
			reader.IsDBNull(8) ? null : reader.GetBoolean(8),
			reader.IsDBNull(9) ? null : reader.GetBoolean(9),
			reader.IsDBNull(10) ? null : reader.GetBoolean(10),
			reader.IsDBNull(11) ? null : reader.GetBoolean(11),
			reader.IsDBNull(12) ? null : reader.GetBoolean(12),
			reader.IsDBNull(13) ? null : reader.GetInt64(13),
			reader.GetDateTime(14),
			reader.GetDateTime(15),
			reader.IsDBNull(16) ? null : reader.GetDateTime(16));
	}

	private sealed record Member(
		Guid MemberId,
		Guid SystemId,
		string DisplayName,
		string? Pronouns,
		string? Description,
		string? Color,
		bool? IsArchived,
		string? ArchivedReason,
		bool? IsPrivate,
		bool? PreventTrusted,
		bool? PreventsFrontNotifications,
		bool? ReceiveMessageBoardNotifications,
		bool? SupportsDescriptionMarkdown,
		long? LastOperationTimeMs,
		DateTime ImportedAtUtc,
		DateTime CreatedAtUtc,
		DateTime? UpdatedAtUtc);
}