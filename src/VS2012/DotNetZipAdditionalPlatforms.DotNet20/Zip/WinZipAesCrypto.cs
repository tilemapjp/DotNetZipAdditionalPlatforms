namespace DotNetZipAdditionalPlatforms.Zip
{
    using System;
    using System.IO;
    using System.Security.Cryptography;

    /// <summary>
    /// This is a helper class supporting WinZip AES encryption.
    /// This class is intended for use only by the DotNetZip library.
    /// </summary>
    /// 
    /// <remarks>
    /// Most uses of the DotNetZip library will not involve direct calls into
    /// the WinZipAesCrypto class.  Instead, the WinZipAesCrypto class is
    /// instantiated and used by the ZipEntry() class when WinZip AES
    /// encryption or decryption on an entry is employed.
    /// </remarks>
    internal class WinZipAesCrypto
    {
        private bool cryptoGeneratedField;
        internal byte[] generatedPvField;
        private byte[] keyBytesField;
        internal int keyStrengthInBitsField;
        private byte[] macInitializationVectorField;
        private string passwordField;
        internal byte[] providedPvField;
        internal byte[] saltField;
        private byte[] storedMacField;
        public byte[] calculatedMacField;
        private short passwordVerificationGeneratedField;
        private short passwordVerificationStoredField;
        private int rfc2898KeygenIterationsField = 0x3e8;

        private WinZipAesCrypto(string password, int KeyStrengthInBits)
        {
            this.passwordField = password;
            this.keyStrengthInBitsField = KeyStrengthInBits;
        }

        private void _GenerateCryptoBytes()
        {
            Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(this.passwordField, this.Salt, this.rfc2898KeygenIterationsField);
            this.keyBytesField = bytes.GetBytes(this._KeyStrengthInBytes);
            this.macInitializationVectorField = bytes.GetBytes(this._KeyStrengthInBytes);
            this.generatedPvField = bytes.GetBytes(2);
            this.cryptoGeneratedField = true;
        }

        public static WinZipAesCrypto Generate(string password, int KeyStrengthInBits)
        {
            WinZipAesCrypto crypto = new WinZipAesCrypto(password, KeyStrengthInBits);
            int num = crypto._KeyStrengthInBytes / 2;
            crypto.saltField = new byte[num];
            new Random().NextBytes(crypto.saltField);
            return crypto;
        }

        public void ReadAndVerifyMac(Stream s)
        {
            bool flag = false;
            this.storedMacField = new byte[10];
            s.Read(this.storedMacField, 0, this.storedMacField.Length);
            if (this.storedMacField.Length != this.calculatedMacField.Length)
            {
                flag = true;
            }
            if (!flag)
            {
                for (int i = 0; i < this.storedMacField.Length; i++)
                {
                    if (this.storedMacField[i] != this.calculatedMacField[i])
                    {
                        flag = true;
                    }
                }
            }
            if (flag)
            {
                throw new BadStateException("The MAC does not match.");
            }
        }

        public static WinZipAesCrypto ReadFromStream(string password, int KeyStrengthInBits, Stream s)
        {
            WinZipAesCrypto crypto = new WinZipAesCrypto(password, KeyStrengthInBits);
            int num = crypto._KeyStrengthInBytes / 2;
            crypto.saltField = new byte[num];
            crypto.providedPvField = new byte[2];
            s.Read(crypto.saltField, 0, crypto.saltField.Length);
            s.Read(crypto.providedPvField, 0, crypto.providedPvField.Length);
            crypto.passwordVerificationStoredField = (short) (crypto.providedPvField[0] + (crypto.providedPvField[1] * 0x100));
            if (password != null)
            {
                crypto.passwordVerificationGeneratedField = (short) (crypto.GeneratedPV[0] + (crypto.GeneratedPV[1] * 0x100));
                if (crypto.passwordVerificationGeneratedField != crypto.passwordVerificationStoredField)
                {
                    throw new BadPasswordException("bad password");
                }
            }
            return crypto;
        }

        private int _KeyStrengthInBytes
        {
            get
            {
                return (this.keyStrengthInBitsField / 8);
            }
        }

        public byte[] GeneratedPV
        {
            get
            {
                if (!this.cryptoGeneratedField)
                {
                    this._GenerateCryptoBytes();
                }
                return this.generatedPvField;
            }
        }

        public byte[] KeyBytes
        {
            get
            {
                if (!this.cryptoGeneratedField)
                {
                    this._GenerateCryptoBytes();
                }
                return this.keyBytesField;
            }
        }

        public byte[] MacIv
        {
            get
            {
                if (!this.cryptoGeneratedField)
                {
                    this._GenerateCryptoBytes();
                }
                return this.macInitializationVectorField;
            }
        }

        public string Password
        {
            private get
            {
                return this.passwordField;
            }
            set
            {
                this.passwordField = value;
                if (this.passwordField != null)
                {
                    this.passwordVerificationGeneratedField = (short) (this.GeneratedPV[0] + (this.GeneratedPV[1] * 0x100));
                    if (this.passwordVerificationGeneratedField != this.passwordVerificationStoredField)
                    {
                        throw new BadPasswordException();
                    }
                }
            }
        }

        public byte[] Salt
        {
            get
            {
                return this.saltField;
            }
        }

        public int SizeOfEncryptionMetadata
        {
            get
            {
                return (((this._KeyStrengthInBytes / 2) + 10) + 2);
            }
        }
    }
}

