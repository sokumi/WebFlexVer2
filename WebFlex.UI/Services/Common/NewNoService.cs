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
        var nos = await NewNosAsync(prefix, 1);
        return nos[0];
    }

    public async Task<List<string>> NewNosAsync(string prefix, int count) {
        if (string.IsNullOrWhiteSpace(prefix)) {
            throw new ArgumentException("prefix 값이 없습니다.", nameof(prefix));
        }

        if (count <= 0) {
            return new List<string>();
        }

        prefix = prefix.Trim().ToUpperInvariant();

        var now = DateTime.Now;
        var datePart = now.ToString("yyMMdd");
        var utcNow = DateTime.UtcNow;

        var newNo = await _db.Set<SNewNo>()
            .FirstOrDefaultAsync(x => x.PREFIX == prefix && x.DATE_PART == datePart);

        if (newNo == null) {
            newNo = new SNewNo {
                ID = $"{prefix}{datePart}",
                PREFIX = prefix,
                DATE_PART = datePart,
                CURRENT_NO = 0,
                IsEnabled = true,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };

            _db.Set<SNewNo>().Add(newNo);
        }

        var startNo = newNo.CURRENT_NO + 1;
        var endNo = newNo.CURRENT_NO + count;

        if (endNo > 9999) {
            throw new InvalidOperationException($"{prefix}{datePart} 번호가 9999번을 초과했습니다.");
        }

        newNo.CURRENT_NO = endNo;
        newNo.UpdatedAt = utcNow;

        await _db.SaveChangesAsync();

        var result = new List<string>();

        for (var no = startNo; no <= endNo; no++) {
            result.Add($"{prefix}{datePart}{no:0000}");
        }

        return result;
    }
}