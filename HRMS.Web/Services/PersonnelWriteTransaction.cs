using System.Data;
using HRMS.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Web.Services;

// All personnel document numbers are allocated under the same transaction lock.
public static class PersonnelWriteTransaction
{
    public static async Task Execute(HrmsContext db,Func<Task> action)
    {
        var existing=db.Database.CurrentTransaction;
        await using var owned=existing==null ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var transaction=existing??owned!;
        var savepoint="HRMS_"+Guid.NewGuid().ToString("N")[..24];
        if(existing!=null) await transaction.CreateSavepointAsync(savepoint);
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @r int;
                EXEC @r=sys.sp_getapplock @Resource=N'HRMS.Personnel.Write',
                    @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
                IF @r<0 THROW 51020, N'Không lấy được khóa nghiệp vụ.', 1;
                """);
            db.ChangeTracker.Clear();
            await action();
            await db.SaveChangesAsync();
            if(owned!=null) await owned.CommitAsync();
        }
        catch
        {
            if(owned!=null) await owned.RollbackAsync();
            else await transaction.RollbackToSavepointAsync(savepoint);
            db.ChangeTracker.Clear();throw;
        }
    }
}
