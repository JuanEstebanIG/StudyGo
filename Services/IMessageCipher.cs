// ============================================================================
// StudyGo · Services/IMessageCipher.cs
// El dominio guarda ChatMessage.EncryptedContent cifrado. El cifrado real vive
// en Infrastructure (§2, "cifrado") y NO es del módulo de Jaison.
// Para no bloquear el chat, aquí va una implementación NoOp PROVISIONAL.
// TODO (dueño de Infraestructura): sustituir NoOpMessageCipher por el cifrado
// real (p. ej. AES con clave por institución) y registrarlo en Program.cs.
// ============================================================================
namespace StudyGo.Services
{
    public interface IMessageCipher
    {
        string Encrypt(string plaintext);
        string Decrypt(string ciphertext);
    }

    /// <summary>PROVISIONAL: passthrough. No cifra. Sustituir por el real.</summary>
    public class NoOpMessageCipher : IMessageCipher
    {
        public string Encrypt(string plaintext) => plaintext ?? string.Empty;
        public string Decrypt(string ciphertext) => ciphertext ?? string.Empty;
    }
}
