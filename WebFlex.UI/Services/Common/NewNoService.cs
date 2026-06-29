using Microsoft.EntityFrameworkCore;
using WebFlex.Shared.Entities.System;
using WebFlex.UI.Data;

namespace WebFlex.UI.Services;

public interface INewNoService {
    Task<string> NewNoAsync(string prefix);
    Task<List<string>> NewNosAsync(string prefix, int count);
}

public class NewNoService : INewNoService {
    private readonly WebFlexDbContext _db;

    public NewNoService(WebFlexDbContext db) {
        _db = db;
    }

    public async Task<string> NewNoAsync(string prefix) {
        var list = await NewNosAsync(prefix, 1);
        return list[0];
    }

    public async Task<List<string>> NewNosAsync(string prefix, int count) {
        if (string.IsNullOrWhiteSpace(prefix)) {
            throw new ArgumentException("prefix 값이 비어 있습니다.", nameof(prefix));
        }

        if (count <= 0) {
            return new List<string>();
        }

        prefix = prefix.Trim().ToUpperInvariant();

        var datePart = DateTime.Now.ToString("yyMMdd");
        var noId = $"{prefix}{datePart}";
        var now = DateTime.UtcNow;

        var sequence = await _db.Set<SNewNo>()
            .FromSqlInterpolated($"""
                SELECT *
                FROM s_new_no
                WHERE no_id = {noId}
                FOR UPDATE
                """)
            .FirstOrDefaultAsync();

        if (sequence == null) {
            sequence = new SNewNo {
                ID = noId,
                PREFIX = prefix,
                DATE_PART = datePart,
                CURRENT_NO = 0,
                IsEnabled = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Set<SNewNo>().Add(sequence);
            await _db.SaveChangesAsync();

            sequence = await _db.Set<SNewNo>()
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM s_new_no
                    WHERE no_id = {noId}
                    FOR UPDATE
                    """)
                .FirstAsync();
        }

        var startNo = sequence.CURRENT_NO + 1;
        var endNo = sequence.CURRENT_NO + count;

        sequence.CURRENT_NO = endNo;
        sequence.UpdatedAt = now;

        await _db.SaveChangesAsync();

        var result = new List<string>();

        for (var i = startNo; i <= endNo; i++) {
            result.Add($"{noId}{i:000}");
        }

        return result;
    }
}