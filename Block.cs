using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace WindowsFormsApp1
{
    [Serializable]
    public class Block
    {
        public int index;
        public string data;
        public long nonce = 0;
        public DateTime timestamp;
        public byte[] hash = new byte[32];
        public byte[] prevHash = new byte[32];
        public int diff;

        public Block(int difficulty, string name)
        {
            index = 0;
            timestamp = DateTime.Now;
            data = "Block #" + index.ToString() + ", sent by: " + name;
            for (int i = 0; i < 32; i++)
            {
                prevHash[i] = 0;
            }
            difficulty = diff;
            sha256();
        }

        public Block(int index, string data, DateTime timestamp,int diff, long nonce,byte[] hash, byte[] prevHash)
        {
            this.index = index;
            this.data = data;
            this.timestamp = timestamp;
            this.diff = diff;
            this.nonce = nonce;
            this.hash = hash;
            this.prevHash = prevHash;
        }

        public Block(Block prev, int diffp, string name)
        {
            diff = diffp;
            timestamp = DateTime.Now;
            index = prev.index + 1;
            data = "Block #" + index.ToString() + ", sent by: " + name;
            prevHash = prev.hash;
            sha256();
        }

        public void sha256()
        {
            string info = nonce.ToString() + index.ToString() + data + timestamp.ToString() + prevHash;
            using (SHA256 sha256Hash = SHA256.Create())
            {
                hash = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(info));
            }
        }
        
        public bool equalTo(Block block)
        {
            return (index == block.index && data == block.data && timestamp == block.timestamp && diff == block.diff && hash.SequenceEqual(block.hash) && prevHash.SequenceEqual(block.prevHash) && nonce == block.nonce);
        }

        public bool isValidHash()
        {
            string hash_str = BitConverter.ToString(hash).Replace("-", "");
            for (int i = 0; i < diff; i++)
            {
                if (hash_str[i] != '0')
                {
                    return false;
                }
            }
            return true;
        }

        public string blockToString()
        {
            string info = "Index: " + index.ToString() + "\r\nData: " + data + "\r\nTimestamp: " + timestamp.ToString()
                + "\r\nDifficulty: " + diff.ToString() + "\r\nNonce: " + nonce.ToString() + "\r\nBlock hash: " + BitConverter.ToString(hash).Replace("-","")
                + "\r\nPrevious block hash: " + BitConverter.ToString(prevHash).Replace("-","") + "\r\n\r\n";
            return info;
        }
    }
}
