using System.Security.Cryptography;
using DevHabit.Api.Services;
using DevHabit.Api.Settings;
using Microsoft.Extensions.Options;

namespace DevHabit.UnitTests.Services;

public sealed class EncryptionServiceTests
{
    private readonly EncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        IOptions<EncryptionOptions> options = Options.Create(new EncryptionOptions
        {
            Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        });

        _encryptionService = new EncryptionService(options);
    }

    [Fact]
    public void Encrypt_ShouldReturnDifferentCiphertext_WhenEncryptingSameText()
    {
        // Arrange
        const string plainText = "sensitive data";

        // Act
        string firstCiphertext = _encryptionService.Encrypt(plainText);
        string secondCiphertext = _encryptionService.Encrypt(plainText);

        // Assert
        Assert.NotEqual(firstCiphertext, secondCiphertext);
    }

    [Fact]
    public void Decrypt_ShouldReturnPlainText_WhenDecryptingCorrectCiphertext()
    {
        // Arrange
        const string plainText = "sensitive data";
        string ciphertext = _encryptionService.Encrypt(plainText);

        // Act
        string decryptedCiphertext = _encryptionService.Decrypt(ciphertext);

        // Assert
        Assert.Equal(plainText, decryptedCiphertext);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-base64")]
    [InlineData("aW52YWxpZC1jaXBoZXJ0ZXh0")] // too short, missing IV
    public void Decrypt_ShouldThrowInvalidOperationException_WhenCiphertextIsInvalid(string invalidCiphertext)
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _encryptionService.Decrypt(invalidCiphertext));
    }

    [Fact]
    public void Decrypt_ShouldThrowInvalidOperationException_WhenCiphertextIsCorrupted()
    {
        // Arrange
        const string plainText = "sensitive data";
        string ciphertext = _encryptionService.Encrypt(plainText);
        string corruptedCiphertext = ciphertext[..^10] + new string('0', 10); // Corrupt last 10 characters

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _encryptionService.Decrypt(corruptedCiphertext));
    }

    [Fact]
    public void Encrypt_ShouldHandleLongText()
    {
        // Arrange
        string longText = new string('a', 10000);

        // Act
        string ciphertext = _encryptionService.Encrypt(longText);
        string decryptedText = _encryptionService.Decrypt(ciphertext);

        // Assert
        Assert.Equal(longText, decryptedText);
    }

    [Fact]
    public void Encrypt_ShouldHandleSpecialCharacters()
    {
        // Arrange
        const string specialChars = "!@#$%^&*()_+-=[]{}|;:'\",.<>?/~`";

        // Act
        string ciphertext = _encryptionService.Encrypt(specialChars);
        string decryptedText = _encryptionService.Decrypt(ciphertext);

        // Assert
        Assert.Equal(specialChars, decryptedText);
    }
}
