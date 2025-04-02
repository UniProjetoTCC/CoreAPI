namespace Business.Enums
{
    [Flags]
    public enum LinkedUserPermissionsEnum
    {
        Transaction = 1,
        Report = 2,
        Product = 4,
        Stock = 8,
        Promotion = 16
    }
}
