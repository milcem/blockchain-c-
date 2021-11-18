using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            label3.Visible = false;
            label2.Visible = true;
        }

        private string senderName = "";
        private IPAddress ipadd = IPAddress.Parse("127.0.0.1");
        bool running = false;
        public int generator = 10; 

        Blockchain block = new Blockchain();
        Thread blocksThread = null;
        int difficulty = 3;
        int changes = 10;
        int adjdiff = 5;
        bool mining = false;

        private TcpClient clientListener = null;
        NetworkStream sendStream = null;
        Thread senderThread = null;

        int portNum;
        private TcpListener accept = null;
        Thread acceptThread = null;
        LinkedList<int> conns = new LinkedList<int>();

        DateTime lastDate;
        private void connect_Click(object sender, EventArgs e)
        {
            running = true;
            lastDate = DateTime.Now;
            try
            {
                portNum = Int32.Parse(textBox3.Text);
                senderName = nodeName.Text;
                accept = new TcpListener(ipadd, portNum);
                accept.Start();
                label3.Visible = true;
                label3.Text = "CONNECTED Port : " + portNum;
                senderThread = new Thread(newChanges);
                senderThread.Start();
                acceptThread = new Thread(acceptChanges);
                acceptThread.Start();
                mining = true;
                blocksThread = new Thread(generateBlock);
                blocksThread.Start();
                Action action = () => textBox1.Text += "Mining started . . \r\n";
                this.Invoke(action);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error! " + ex.ToString());
            }
        }
        
        private void connectPort_Click(object sender, EventArgs e)
        {
            try
            {
                conns.AddLast(Int32.Parse(port.Text));
                clientListener = new TcpClient();
                clientListener.Connect(ipadd, Int32.Parse(port.Text));
                port.Text = "";

                senderThread = new Thread(newChanges);
                senderThread.Start();
                sendStream = clientListener.GetStream();

                string msg = "*" + senderName + " has connected";
                byte[] data = Encoding.UTF8.GetBytes(msg);
                sendStream.Write(data, 0, data.Length);
                sendStream.Close();
             }
            catch(Exception ex)
            {
                MessageBox.Show("Error! " + ex.ToString());
            }
        }
        
        private void newChanges()
        {
            Blockchain accepted = new Blockchain();
            while (true)
            {
                byte[] chainInfo = new byte[4096];
                int start = 0;
                try
                {
                    TcpClient client = accept.AcceptTcpClient();
                    NetworkStream clientStream = client.GetStream();
                    int bytes = clientStream.Read(chainInfo, 0, chainInfo.Length);
                    if (chainInfo[0] == '*')
                    {
                        string msg = Encoding.UTF8.GetString(chainInfo, 0, bytes);
                        Action action = () => textBox1.Text += msg + "\r\n";
                        this.Invoke(action);
                        continue;
                    }
                    clientStream.Close();
                    while (true)
                    {
                        byte[] blockInfo = new byte[128];
                        Array.Copy(chainInfo, start, blockInfo,0, 128);
                        start += 128;
                        if (blockInfo[0] == '#')
                        {
                            if (!accepted.validateChain())
                            {
                                accepted.chain.Clear();
                                break;
                            }
                            Action action = () => textBox1.Text += "Difficulty of sender's blockchain: " + accepted.cumulativeDiff() + "\r\n My chain: " + block.cumulativeDiff() + "\r\n";
                            this.Invoke(action);
                            if (block.areEqual(block.chain, accepted.chain))
                            {
                                Action actionn = () => textBox1.Text += "Blockchains are equal! \r\n";
                                this.Invoke(actionn);
                            }
                            if (!(block.areEqual(block.chain, accepted.chain)))
                            {
                                if (block.cumulativeDiff() <= accepted.cumulativeDiff())
                                {
                                    Action actionn = () => textBox1.Text += "Correcting block chain .. \r\n";
                                    this.Invoke(actionn);
                                    mining = false;
                                    difficulty = accepted.chain.Last.Value.diff;
                                    block.chain = new LinkedList<Block>(accepted.chain);
                                    mining = true;
                                    printChain(block);
                                }
                            }
                            accepted.chain.Clear();
                            break;
                        }
                        int index = BitConverter.ToInt32(blockInfo, 0);
                        string data = Encoding.UTF8.GetString(blockInfo, 4, 32).Replace("\0", String.Empty);
                        DateTime timestamp = new DateTime(BitConverter.ToInt64(blockInfo, 36));
                        int diff = BitConverter.ToInt32(blockInfo, 44);
                        long nonce = BitConverter.ToInt64(blockInfo, 48);
                        byte[] hash = new byte[32];
                        Array.Copy(blockInfo, 56, hash, 0, 32);
                        byte[] prevHash = new byte[32];
                        Array.Copy(blockInfo, 88, prevHash, 0, 32);
                        Block tmp = new Block(index, data, timestamp, diff, nonce, hash, prevHash);
                        accepted.chain.AddLast(tmp);
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Napaka! " + ex.ToString());
                }
            }
        }

        void acceptChanges()
        {
            while (running) {
                if ((DateTime.Now - lastDate).Seconds < changes)
                    continue;
                lastDate = DateTime.Now;
                try
                {
                    byte[] chainInfo = new byte[4096];
                    int start = 0;
                    foreach(Block blocks in block.chain) {
                        byte[] bInfo = new byte[128];
                        byte[] index = BitConverter.GetBytes(blocks.index);
                        index.CopyTo(bInfo, 0);
                        byte[] data = Encoding.UTF8.GetBytes(blocks.data);
                        data.CopyTo(bInfo, 4);
                        byte[] timestamp = BitConverter.GetBytes(blocks.timestamp.Ticks);
                        timestamp.CopyTo(bInfo, 36);
                        byte[] diff = BitConverter.GetBytes(blocks.diff);
                        diff.CopyTo(bInfo, 44);
                        byte[] nonce = BitConverter.GetBytes(blocks.nonce);
                        nonce.CopyTo(bInfo, 48);
                        blocks.hash.CopyTo(bInfo, 56);
                        blocks.prevHash.CopyTo(bInfo, 88);
                        bInfo.CopyTo(chainInfo, start);
                        start += 128;
                    }
                    byte[] end = Encoding.UTF8.GetBytes("#");
                    end.CopyTo(chainInfo, start);
                    foreach(int portt in conns)
                    {
                        TcpClient client = new TcpClient();
                        client.Connect(ipadd, portt);
                        NetworkStream clientStream = client.GetStream();
                        clientStream.Write(chainInfo, 0, chainInfo.Length);
                        clientStream.Close();
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error! " + ex.ToString());
                }
            }
        }

        private void printChain(Blockchain chainnn)
        {
            try
            {
                Action action = () => textBox2.Text = "";
                this.Invoke(action);
                foreach(Block block in chainnn.chain)
                {
                    Action action2 = () => textBox2.Text += block.blockToString();
                    this.Invoke(action2);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error! " + ex.ToString());
            }
        }

        void generateBlock()
        {
            while (true)
            {
                if (!mining) continue;
                Block newBlock = null;
                if (block.chain.Count == 0)
                    newBlock = new Block(difficulty, senderName);
                else
                    newBlock = new Block(block.chain.Last(), difficulty, senderName);
                while (!newBlock.isValidHash() && mining)
                {
                    newBlock.nonce++;
                    newBlock.sha256();
                }
                if (!mining) continue;
                if (!block.validateBlock(newBlock))
                    continue;
                block.chain.AddLast(newBlock);
                printChain(block);
                if (block.chain.Count % adjdiff == 0)
                {
                    difficulty = adjustDiff();
                    Action action = () => textBox1.Text += "Difficulty changed: " + difficulty + "\r\n";
                    if (running) this.Invoke(action);
                }
            }
        }

        int adjustDiff()
        {
            Block prev = block.chain.ElementAt(block.chain.Last.Value.index - adjdiff+1);
            int expected = generator * adjdiff;
            int time = (block.chain.Last.Value.timestamp - prev.timestamp).Seconds;
            if (time * 2 <= expected)
                return block.chain.Last.Value.diff + 1;
            else if (time >= expected * 2)
                return block.chain.Last.Value.diff - 1;
            else
                return block.chain.Last.Value.diff;
        }
    }
}
