using System.Data;

namespace Convert_to_dcm.Sql
{
    public interface ISqlService
    {
        DataTable ExecuteSelectQuery(string pid, string modality);
    }
}
