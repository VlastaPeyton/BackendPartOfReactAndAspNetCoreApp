
namespace Api.Value_Objects
{   // Strongly-typed Id. Record, a ne class, jer nema potrebe za class. 
    // Jos uvek necu koristiti ovo, ali ovako treba u DDD.
    public record StockId
    {
        public int Value { get; } // Nije Guid, jer app radi kada Id of Stock is int. Pa da ne moram brisati sve podatke i migrirati opet sve. ALi treba Guid biti u praksi.

        private StockId(int value) => Value = value; // Konstruktor na moderan nacin jer imam 1 polje samo

        // Of umesto public construktora je Rich domain model
        public static StockId Of(int value) // Objasnjenje kao za CommentId
        {
            ArgumentNullException.ThrowIfNull(value); // Ovo je Domain layer i mora validacija u njemu zbog rich domain model
            return new StockId(value);
        }
    }
}
