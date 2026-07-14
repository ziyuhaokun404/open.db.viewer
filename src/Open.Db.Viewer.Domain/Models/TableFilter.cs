namespace Open.Db.Viewer.Domain.Models;

public enum TableFilterOperator
{
    Contains = 0,
    Equals = 1,
    IsNull = 2,
    IsNotNull = 3
}

/// <summary>
/// 数据页列筛选条件。列名由调用方校验为真实列，值通过参数绑定。
/// </summary>
public sealed record TableFilter(
    string Column,
    TableFilterOperator Operator,
    string? Value = null);
