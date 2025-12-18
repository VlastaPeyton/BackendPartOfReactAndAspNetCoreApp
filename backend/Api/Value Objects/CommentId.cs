namespace Api.Value_Objects
{
    // Strongly-typed Id. Record, a ne class, jer nema potrebe za class. DDD tj rich domain model je ovo
    public record CommentId
    {   
        public int Value { get; } // Nije Guid, jer app radi kada Id of Comment is int. Pa da ne moram brisati sve podatke i migrirati opet sve.
        
        // Private ctor zbog DDD
        private CommentId(int value) => Value = value; // Konstruktor na moderan nacin jer imam 1 polje samo
        
        // Of umesto public construktora je Rich domain model koristim
        public static CommentId Of(int value) // Koristim u CommentRepository kada trazim c.Id = CommentId.Of(id) jer tako primeni HasConversion kad cita iz baze sto je i def u OnModelCreating
        {
            /* Validacija za ValueObject se radi unutar Value Object, ali necu je raditi jer u CommentRepository.Create metodi 
             logika je takva da EF ChangeTracker generise random Id (koji je obicno negativan ili 0) i onda nakon SaveChangesAsync 
             u CommentService/CQRS Create metodi baza generise pravu vrednost koje ChangeTracker odma vidi. Ako uradim validaciju da 
             id mora >=0 bice uvek error.*/
            return new CommentId(value);
        }
    }
}
