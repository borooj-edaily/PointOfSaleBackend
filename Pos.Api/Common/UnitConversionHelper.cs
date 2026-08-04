using Pos.Api.Enums;

namespace Pos.Api.Common
{
    public static class UnitConversionHelper
    {
        /// <summary>
        /// يحوّل كمية مُدخلة (بالقطعة أو بالعبوة) إلى عدد الحبات المكافئ،
        /// وهي الوحدة الموحّدة المخزّنة دايماً بـ StockInPieces.
        /// </summary>
        public static int ConvertToPieces(int quantity, bool isPackage, int? piecesPerPackage)
        {
            if (!isPackage)
                return quantity;

            if (piecesPerPackage is null or <= 0)
                throw new InvalidOperationException(
                    "لا يمكن التحويل إلى حبات: عدد الحبات بالعبوة غير محدد لهذا الصنف.");

            return quantity * piecesPerPackage.Value;
        }
    }
}