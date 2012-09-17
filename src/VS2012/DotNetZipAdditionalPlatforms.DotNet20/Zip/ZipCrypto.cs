namespace DotNetZipAdditionalPlatforms.Zip
{
    using DotNetZipAdditionalPlatforms.Crc;
    using System;
    using System.IO;

    /// <summary>
    /// This class implements the "traditional" or "classic" PKZip encryption,
    /// which today is considered to be weak. On the other hand it is
    /// ubiquitous. This class is intended for use only by the DotNetZip
    /// library.
    /// </summary>
    /// 
    /// <remarks>
    /// Most uses of the DotNetZip library will not involve direct calls into
    /// the ZipCrypto class.  Instead, the ZipCrypto class is instantiated and
    /// used by the ZipEntry() class when encryption or decryption on an entry
    /// is employed.  If for some reason you really wanted to use a weak
    /// encryption algorithm in some other application, you might use this
    /// library.  But you would be much better off using one of the built-in
    /// strong encryption libraries in the .NET Framework, like the AES
    /// algorithm or SHA.
    /// </remarks>
    internal class ZipCrypto
    {
        private uint[] keysField = new uint[] { 0x12345678, 0x23456789, 0x34567890 };
        private CRC32 crc32Field = new CRC32();

        /// <summary>
        /// The default constructor for ZipCrypto.
        /// </summary>
        /// 
        /// <remarks>
        /// This class is intended for internal use by the library only. It's
        /// probably not useful to you. Seriously.  Stop reading this
        /// documentation.  It's a waste of your time.  Go do something else.
        /// Check the football scores. Go get an ice cream with a friend.
        /// Seriously.
        /// </remarks>
        private ZipCrypto()
        {
        }

        /// <summary>
        /// Call this method on a cipher text to render the plaintext. You must
        /// first initialize the cipher with a call to InitCipher.
        /// </summary>
        /// 
        /// <example>
        /// <code>
        /// var cipher = new ZipCrypto();
        /// cipher.InitCipher(Password);
        /// // Decrypt the header.  This has a side effect of "further initializing the
        /// // encryption keys" in the traditional zip encryption.
        /// byte[] DecryptedMessage = cipher.DecryptMessage(EncryptedMessage);
        /// </code>
        /// </example>
        /// 
        /// <param name="cipherText">The encrypted buffer.</param>
        /// <param name="length">
        /// The number of bytes to encrypt.
        /// Should be less than or equal to CipherText.Length.
        /// </param>
        /// 
        /// <returns>The plaintext.</returns>
        public byte[] DecryptMessage(byte[] cipherText, int length)
        {
            if (cipherText == null)
            {
                throw new ArgumentNullException("cipherText");
            }
            if (length > cipherText.Length)
            {
                throw new ArgumentOutOfRangeException("length", "Bad length during Decryption: the length parameter must be smaller than or equal to the size of the destination array.");
            }
            byte[] buffer = new byte[length];
            for (int i = 0; i < length; i++)
            {
                byte byteValue = (byte) (cipherText[i] ^ this.MagicByte);
                this.UpdateKeys(byteValue);
                buffer[i] = byteValue;
            }
            return buffer;
        }

        /// <summary>
        /// This is the converse of DecryptMessage.  It encrypts the plaintext
        /// and produces a ciphertext.
        /// </summary>
        /// 
        /// <param name="plainText">The plain text buffer.</param>
        /// 
        /// <param name="length">
        /// The number of bytes to encrypt.
        /// Should be less than or equal to plainText.Length.
        /// </param>
        /// 
        /// <returns>The ciphertext.</returns>
        public byte[] EncryptMessage(byte[] plainText, int length)
        {
            if (plainText == null)
            {
                throw new ArgumentNullException("plaintext");
            }
            if (length > plainText.Length)
            {
                throw new ArgumentOutOfRangeException("length", "Bad length during Encryption: The length parameter must be smaller than or equal to the size of the destination array.");
            }
            byte[] buffer = new byte[length];
            for (int i = 0; i < length; i++)
            {
                byte byteValue = plainText[i];
                buffer[i] = (byte) (plainText[i] ^ this.MagicByte);
                this.UpdateKeys(byteValue);
            }
            return buffer;
        }

        public static ZipCrypto ForRead(string password, ZipEntry e)
        {
            Stream s = e.archiveStreamField;
            e.weakEncryptionHeaderField = new byte[12];
            byte[] buffer = e.weakEncryptionHeaderField;
            ZipCrypto crypto = new ZipCrypto();
            if (password == null)
            {
                throw new BadPasswordException("This entry requires a password.");
            }
            crypto.InitCipher(password);
            ZipEntry.ReadWeakEncryptionHeader(s, buffer);
            byte[] buffer2 = crypto.DecryptMessage(buffer, buffer.Length);
            if (buffer2[11] != ((byte) ((e.crc32Field >> 0x18) & 0xff)))
            {
                if ((e.bitFieldField & 8) != 8)
                {
                    throw new BadPasswordException("The password did not match.");
                }
                if (buffer2[11] != ((byte) ((e.timeBlobField >> 8) & 0xff)))
                {
                    throw new BadPasswordException("The password did not match.");
                }
            }
            return crypto;
        }

        public static ZipCrypto ForWrite(string password)
        {
            ZipCrypto crypto = new ZipCrypto();
            if (password == null)
            {
                throw new BadPasswordException("This entry requires a password.");
            }
            crypto.InitCipher(password);
            return crypto;
        }

        /// <summary>
        /// This initializes the cipher with the given password.
        /// See AppNote.txt for details.
        /// </summary>
        /// 
        /// <param name="passphrase">
        /// The passphrase for encrypting or decrypting with this cipher.
        /// </param>
        /// 
        /// <remarks>
        /// <code>
        /// Step 1 - Initializing the encryption keys
        /// -----------------------------------------
        /// Start with these keys:
        /// Key(0) := 305419896 (0x12345678)
        /// Key(1) := 591751049 (0x23456789)
        /// Key(2) := 878082192 (0x34567890)
        /// 
        /// Then, initialize the keys with a password:
        /// 
        /// loop for i from 0 to length(password)-1
        /// update_keys(password(i))
        /// end loop
        /// 
        /// Where update_keys() is defined as:
        /// 
        /// update_keys(char):
        /// Key(0) := crc32(key(0),char)
        /// Key(1) := Key(1) + (Key(0) bitwiseAND 000000ffH)
        /// Key(1) := Key(1) * 134775813 + 1
        /// Key(2) := crc32(key(2),key(1) rightshift 24)
        /// end update_keys
        /// 
        /// Where crc32(old_crc,char) is a routine that given a CRC value and a
        /// character, returns an updated CRC value after applying the CRC-32
        /// algorithm described elsewhere in this document.
        /// 
        /// </code>
        /// 
        /// <para>
        /// After the keys are initialized, then you can use the cipher to
        /// encrypt the plaintext.
        /// </para>
        /// 
        /// <para>
        /// Essentially we encrypt the password with the keys, then discard the
        /// ciphertext for the password. This initializes the keys for later use.
        /// </para>
        /// 
        /// </remarks>
        public void InitCipher(string passphrase)
        {
            byte[] buffer = SharedUtilities.StringToByteArray(passphrase);
            for (int i = 0; i < passphrase.Length; i++)
            {
                this.UpdateKeys(buffer[i]);
            }
        }

        private void UpdateKeys(byte byteValue)
        {
            this.keysField[0] = (uint) this.crc32Field.ComputeCrc32((int) this.keysField[0], byteValue);
            this.keysField[1] += (byte) this.keysField[0];
            this.keysField[1] = (this.keysField[1] * 0x8088405) + 1;
            this.keysField[2] = (uint) this.crc32Field.ComputeCrc32((int) this.keysField[2], (byte) (this.keysField[1] >> 0x18));
        }

        /// <summary>
        /// From AppNote.txt:
        /// unsigned char decrypt_byte()
        /// local unsigned short temp
        /// temp :=- Key(2) | 2
        /// decrypt_byte := (temp * (temp ^ 1)) bitshift-right 8
        /// end decrypt_byte
        /// </summary>
        private byte MagicByte
        {
            get
            {
                ushort num = (ushort) (((ushort) (this.keysField[2] & 0xffff)) | 2);
                return (byte) ((num * (num ^ 1)) >> 8);
            }
        }
    }
}

