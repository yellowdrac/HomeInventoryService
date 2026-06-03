using System.Security.Cryptography;
using HomeInventory.Application.Common.Abstractions;

namespace HomeInventory.Application.Common.Identity;

/// <summary>
/// Generates 8-character join codes from an unambiguous alphabet (no <c>0/O</c> or <c>1/I</c>)
/// using a cryptographically secure random source.
/// </summary>
public sealed class JoinCodeGenerator : IJoinCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int Length = 8;

    public string Generate()
    {
        Span<char> buffer = stackalloc char[Length];
        for (var i = 0; i < Length; i++)
        {
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(buffer);
    }
}
