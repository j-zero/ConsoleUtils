using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace privatebin
{
    internal class Program
    {
        static void Main(string[] args)
        {
            /* --- Encryption parameters ---
            $CIPHER_ITER_COUNT = 100000;
            $CIPHER_SALT_BYTES = 8;
            $CIPHER_BLOCK_BITS = 256;
            $CIPHER_BLOCK_BYTES = $CIPHER_BLOCK_BITS / 8; // 32 bytes
            $CIPHER_TAG_BITS = $CIPHER_BLOCK_BITS / 2; // 128 bits
            $CIPHER_TAG_BYTES = $CIPHER_TAG_BITS / 8;   // 16 bytes
            $CIPHER_STRONG = true;

            $passbytes = openssl_random_pseudo_bytes($CIPHER_BLOCK_BYTES, $CIPHER_STRONG);
            $passhash = b58($passbytes);

            if(!empty($password)) {
              $pass = $passbytes . $password;
            } else {
              $pass = $passbytes;
            }

            $iv = openssl_random_pseudo_bytes($CIPHER_TAG_BYTES);
            $salt = openssl_random_pseudo_bytes($CIPHER_SALT_BYTES);
            $key = openssl_pbkdf2($pass, $salt, $CIPHER_BLOCK_BYTES,
                                  $CIPHER_ITER_COUNT, 'sha256');

            */

            string password = "test";
            int iterations = 100000;

            byte[] passbytes = GenerateRandomBytes(32);
            byte[] iv = GenerateRandomBytes(16);
            byte[] salt = GenerateRandomBytes(8);

            string passhash = Base58.Encode(passbytes);

            byte[] pass;

            if (!string.IsNullOrEmpty(password))
                pass = MergeByteArrays(passbytes, System.Text.Encoding.UTF8.GetBytes(password)); // TODO: empty password
            else
                pass = passbytes;

            Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(pass, salt, iterations, HashAlgorithmName.SHA256);
            byte[] key = pbkdf2.GetBytes(32); //32 bytes length is 256 bits
            byte[] nonce = pbkdf2.GetBytes(16); //32 bytes length is 256 bits
            byte[] tag = new byte[16];
            string pastedata = "{\"paste\":\"C# Test 123\"}";

            byte[] pData = System.Text.Encoding.UTF8.GetBytes(pastedata);

            $ciphterText = AESGCM.GcmEncrypt(pData,key, nonce, tag,


            Console.WriteLine(passhash);
            Console.ReadLine();

        }

        private static byte[] GenerateRandomBytes(int length)
        {
            var result = new byte[length];
            using (var r = new RNGCryptoServiceProvider()){ r.GetNonZeroBytes(result); }
            return result;
        }

        private static byte[] MergeByteArrays(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
    }
}
