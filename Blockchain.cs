using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsFormsApp1
{
    [Serializable]
    public class Blockchain
    {
        public LinkedList<Block> chain { get; set; }

        public Blockchain()
        {
            chain = new LinkedList<Block>();
        }

        public bool areEqual(LinkedList<Block> one, LinkedList<Block> two)
        {
            if (one.Count != two.Count)
                return false;
            LinkedListNode<Block> node = two.First;
            foreach(Block block in one)
            {
                if (!block.equalTo(node.Value))
                    return false;
                node = node.Next;
            }
            return true;
        }

        public bool validateBlock(Block block)
        {
            if (chain.Count == 0)
                return true;
            if (block.index == chain.Last.Value.index + 1 && block.prevHash.SequenceEqual(chain.Last.Value.hash) && block.hash.SequenceEqual(sha256(block)) && (block.timestamp - chain.Last.Value.timestamp).Minutes < 1 && (block.timestamp - DateTime.Now).Minutes < 1)
                return true;
            return false;
        }

        byte[] sha256(Block block)
        {
            byte[] hash = new byte[32];
            string info = block.nonce.ToString() + block.index.ToString() + block.data
                          + block.timestamp.ToString() + block.prevHash;
            using (SHA256 sha256Hash = SHA256.Create())
            {
                hash = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(info));
            }
            return hash;
        }

        public bool validateChain()
        {
            LinkedListNode<Block> node = chain.First;
            if (node == null || node.Next == null)
                return true;
            node=node.Next;
            while (node != null)
            {
                if(!(node.Value.index==node.Previous.Value.index+1 && node.Value.prevHash.SequenceEqual(node.Previous.Value.hash)&&
                    node.Value.hash.SequenceEqual(sha256(node.Value))&&(node.Previous.Value.timestamp-node.Value.timestamp).Minutes<1 &&
                    (node.Value.timestamp - DateTime.Now).Minutes < 1)){
                    return false;
                }
                node = node.Next;
            }
            return true;
        }

        public int cumulativeDiff()
        {
            int sum = 0;
            foreach(Block block in chain)
            {
                sum += (int)Math.Pow(2, block.diff);
            }
            return sum;
        }
    }
}
