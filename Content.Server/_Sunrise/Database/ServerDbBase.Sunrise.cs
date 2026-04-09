// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using System.Linq;
using System.Threading.Tasks;
using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared._Sunrise.MentorHelp;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Enums;
using Robust.Shared.Network;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    #region Character profile — Sunrise fields

    /// <summary>
    ///     Write direction: applies all Sunrise-specific fields from a character profile
    ///     to the database <see cref="Profile"/> entity.
    ///     Called from <c>ConvertProfiles(HumanoidCharacterProfile, int, Profile?)</c>.
    /// </summary>
    public static void ApplySunriseFieldsToDb(
        Profile profile,
        HumanoidCharacterProfile humanoid,
        HumanoidCharacterAppearance appearance)
    {
        profile.Voice = humanoid.Voice;
        profile.BodyType = humanoid.BodyType;
        profile.Width = appearance.Width;
        profile.Height = appearance.Height;
        profile.HairColorType = (int)appearance.HairMarkingEffectType;
        profile.HairExtendedColor = appearance.HairMarkingEffect?.ToString() ?? "";
        profile.FacialHairColorType = (int)appearance.FacialHairMarkingEffectType;
        profile.FacialHairExtendedColor = appearance.FacialHairMarkingEffect?.ToString() ?? "";
    }

    /// <summary>
    ///     Read direction: resolves the TTS voice string read from the database,
    ///     applying a sex-based default when the stored value is empty.
    ///     Called from <c>ConvertProfiles(Profile)</c>.
    /// </summary>
    public static void ResolveSunriseTTSVoice(string rawVoice, Sex sex, ref string resolvedVoice)
    {
        resolvedVoice = string.IsNullOrEmpty(rawVoice)
            ? SharedHumanoidAppearanceSystem.DefaultSexVoice[sex]
            : rawVoice;
    }

    #endregion

    #region Bans — Sunrise extensions

    public abstract Task<List<ServerBanDef>> GetServerBansByAdminAsync(NetUserId adminId, DateTimeOffset since);
    public abstract Task DeleteServerBanAsync(int banId);

    #endregion

    #region AHelp

    public async Task AddAHelpMessage(Guid senderUserId, Guid receiverUserId, string message, DateTimeOffset sentAt, bool playSound, bool adminOnly)
    {
        await using var db = await GetDb();
        var ahelpMessage = new AHelpMessage
        {
            SenderUserId = senderUserId,
            ReceiverUserId = receiverUserId,
            Message = message,
            SentAt = sentAt,
            PlaySound = playSound,
            AdminOnly = adminOnly
        };
        db.DbContext.AHelpMessages.Add(ahelpMessage);
        await db.DbContext.SaveChangesAsync();
    }

    public async Task<List<AHelpMessage>> GetAHelpMessagesByReceiverAsync(Guid receiverUserId)
    {
        await using var db = await GetDb();

        var messages = await db.DbContext.AHelpMessages
            .Where(m => m.ReceiverUserId == receiverUserId)
            .ToListAsync();

        return messages;
    }

    #endregion

    #region MentorHelp

    public async Task AddMentorHelpTicketAsync(MentorHelpTicket ticket)
    {
        await using var db = await GetDb();
        db.DbContext.MentorHelpTickets.Add(ticket);
        await db.DbContext.SaveChangesAsync();
    }

    public async Task<MentorHelpTicket?> GetMentorHelpTicketAsync(int ticketId)
    {
        await using var db = await GetDb();
        return await db.DbContext.MentorHelpTickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId);
    }

    public async Task<List<MentorHelpStatistics>> GetMentorHelpStatisticsAsync(DateTimeOffset? from)
    {
        await using var db = await GetDb();
        var isSqlite = db.DbContext.Database.ProviderName?.Contains("Sqlite") == true;

        var handledTicketsQuery = db.DbContext.MentorHelpMessages
            .AsNoTracking()
            .Join(
                db.DbContext.MentorHelpTickets.AsNoTracking().Where(ticket => ticket.AssignedToUserId != null),
                message => message.TicketId,
                ticket => ticket.Id,
                (message, ticket) => new
                {
                    message.TicketId,
                    message.SenderUserId,
                    message.SentAt,
                    ticket.PlayerId,
                    AssignedMentorId = ticket.AssignedToUserId!.Value
                })
            .Where(activity =>
                activity.SenderUserId == activity.AssignedMentorId &&
                activity.SenderUserId != activity.PlayerId);

        var messagesQuery = db.DbContext.MentorHelpMessages
            .AsNoTracking()
            .Join(
                db.DbContext.MentorHelpTickets.AsNoTracking(),
                message => message.TicketId,
                ticket => ticket.Id,
                (message, ticket) => new
                {
                    message.SenderUserId,
                    message.SentAt,
                    ticket.PlayerId
                })
            .Where(message => message.SenderUserId != message.PlayerId);

        if (from != null && !isSqlite)
            messagesQuery = messagesQuery.Where(m => m.SentAt >= from);

        var handledTicketsData = await handledTicketsQuery
            .Select(ticket => new { ticket.TicketId, ticket.AssignedMentorId, ticket.SentAt })
            .ToListAsync();

        var messagesData = await messagesQuery
            .Select(m => new { m.SenderUserId, m.SentAt })
            .ToListAsync();

        if (from != null && isSqlite)
            messagesData = messagesData.Where(m => m.SentAt >= from).ToList();

        var handledTickets = handledTicketsData
            .GroupBy(ticket => new { ticket.AssignedMentorId, ticket.TicketId })
            .Select(group => new
            {
                MentorUserId = group.Key.AssignedMentorId,
                FirstHandledAt = group.Min(ticket => ticket.SentAt)
            });

        if (from != null)
            handledTickets = handledTickets.Where(ticket => ticket.FirstHandledAt >= from);

        var ticketStats = handledTickets
            .GroupBy(ticket => ticket.MentorUserId)
            .Select(group => new { MentorUserId = group.Key, TicketsClosed = group.Count() })
            .ToList();

        var messageStats = messagesData
            .GroupBy(message => message.SenderUserId)
            .Select(group => new { MentorUserId = group.Key, MessagesCount = group.Count() })
            .ToList();

        var stats = new Dictionary<Guid, MentorHelpStatistics>();

        foreach (var ticketStat in ticketStats)
        {
            stats[ticketStat.MentorUserId] = new MentorHelpStatistics
            {
                MentorUserId = ticketStat.MentorUserId,
                TicketsClosed = ticketStat.TicketsClosed,
                MessagesCount = 0
            };
        }

        foreach (var messageStat in messageStats)
        {
            if (stats.TryGetValue(messageStat.MentorUserId, out var stat))
            {
                stat.MessagesCount = messageStat.MessagesCount;
                stats[messageStat.MentorUserId] = stat;
            }
            else
            {
                stats[messageStat.MentorUserId] = new MentorHelpStatistics
                {
                    MentorUserId = messageStat.MentorUserId,
                    TicketsClosed = 0,
                    MessagesCount = messageStat.MessagesCount
                };
            }
        }

        return stats.Values.ToList();
    }

    public async Task UpdateMentorHelpTicketAsync(MentorHelpTicket ticket)
    {
        await using var db = await GetDb();
        db.DbContext.MentorHelpTickets.Update(ticket);
        await db.DbContext.SaveChangesAsync();
    }

    public async Task<List<MentorHelpTicket>> GetMentorHelpTicketsByPlayerAsync(Guid playerId)
    {
        await using var db = await GetDb();
        var tickets = await db.DbContext.MentorHelpTickets
            .AsNoTracking()
            .Where(t => t.PlayerId == playerId)
            .ToListAsync();
        return tickets.OrderByDescending(t => t.CreatedAt).ToList();
    }

    public async Task<List<MentorHelpTicket>> GetOpenMentorHelpTicketsAsync()
    {
        await using var db = await GetDb();
        var tickets = await db.DbContext.MentorHelpTickets
            .AsNoTracking()
            .Where(t => t.Status != MentorHelpTicketStatus.Closed)
            .ToListAsync();
        return tickets.OrderByDescending(t => t.UpdatedAt).ToList();
    }

    public async Task<List<MentorHelpTicket>> GetAssignedMentorHelpTicketsAsync(Guid mentorId)
    {
        await using var db = await GetDb();
        var tickets = await db.DbContext.MentorHelpTickets
            .AsNoTracking()
            .Where(t => t.AssignedToUserId == mentorId && t.Status != MentorHelpTicketStatus.Closed)
            .ToListAsync();
        return tickets.OrderByDescending(t => t.UpdatedAt).ToList();
    }

    public async Task<List<MentorHelpTicket>> GetClosedMentorHelpTicketsAsync()
    {
        await using var db = await GetDb();
        var tickets = await db.DbContext.MentorHelpTickets
            .AsNoTracking()
            .Where(t => t.Status == MentorHelpTicketStatus.Closed)
            .ToListAsync();
        return tickets.OrderByDescending(t => t.UpdatedAt).ToList();
    }

    public async Task AddMentorHelpMessageAsync(MentorHelpMessage message)
    {
        await using var db = await GetDb();
        db.DbContext.MentorHelpMessages.Add(message);
        await db.DbContext.SaveChangesAsync();
    }

    public async Task<List<MentorHelpMessage>> GetMentorHelpMessagesByTicketAsync(int ticketId)
    {
        await using var db = await GetDb();
        return await db.DbContext.MentorHelpMessages
            .AsNoTracking()
            .Where(m => m.TicketId == ticketId)
            .ToListAsync();
    }

    #endregion
}
