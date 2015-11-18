using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using System.Data.Common;

using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;

namespace qdf_test_wpf_1
{
    
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            bpGoals = new List<ListBox>();
            bpGoals.Add(ban1);
            bpGoals.Add(ban2);
            bpGoals.Add(pick1);
            bpGoals.Add(pick2);

            this.Closing += doClose;//添加关闭窗口时的行为
            
            try{
                //设定服务器IP地址
                IPAddress ip = IPAddress.Parse("182.92.10.238");
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(new IPEndPoint(ip, 42769)); //配置服务器IP与端口
                receiveThread = new Thread(doReceive);//启动接收服务器消息的线程
                receiveThread.Start();

                doSend("getgames");
            } catch (Exception e) {
                MessageBox.Show("连接服务器失败！");
                this.Close();
            }
        }
        
        private void doClose(object o, System.ComponentModel.CancelEventArgs e) {
            if(clientSocket != null)
            {
                clientSocket.Close();
            }
            if(receiveThread != null)
            {
                receiveThread.Abort();
            }
        }
        
        Thread receiveThread;
        Socket clientSocket;
        byte[] socketResult = new byte[1024];

        void doSend(String content) {
            clientSocket.Send(Encoding.UTF8.GetBytes(content + "\n"));
        }

        //接收服务器消息的线程的线程体
        void doReceive() {
            try
            {
                while (true)
                {
                    int receiveLength = clientSocket.Receive(socketResult);
                    String msg = Encoding.UTF8.GetString(socketResult, 0, receiveLength);
                    dealRequest(msg);
                }
            }
            catch (SocketException se)
            {
                //MessageBox.Show("连接服务器失败！");
                //this.Close();
            }
        }

        private void dealRequest(String msg) {
            Command cmd = Command.fromString(msg);
            switch (cmd.name)
            {
                case "games":
                    this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate ()
                    {
                        gamesBox.Items.Clear();
                        String gamesStr = cmd.attrs["games"];
                        String[] gamesArray = gamesStr.Split('|');
                        foreach (String g in gamesArray)
                        {
                            gamesBox.Items.Add(g);
                        }
                        setStateMsg();
                    });
                    break;
                case "game":
                    turn = int.Parse(cmd.attrs["turn"]);
                    if (playerName == cmd.attrs["p1"])
                    {
                        state = "p1";
                    }
                    else if (cmd.attrs.ContainsKey("p2") && playerName == cmd.attrs["p2"])
                    {
                        state = "p2";
                    }
                    this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate ()
                    {
                        setStateMsg();
                    });
                    break;
                case "bp":
                    turn += 1;
                    String hero = cmd.attrs["hero"];
                    this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate ()
                    {
                        bpGoal.Items.Add(hero);
                        setStateMsg();
                    });
                    break;
                case "gameover":
                    state = "";
                    playerName = null;
                    turn = -1000;
                    bpGoal = null;
                    this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate ()
                    {
                        foreach (ListBox g in bpGoals)
                        {
                            g.Items.Clear();
                            g.Background = Brushes.White;
                        }
                        heroText.Text = "";
                        setStateMsg();
                        MessageBox.Show(cmd.attrs["msg"]);
                    });
                    break;
                case "error":
                    MessageBox.Show(msg);
                    break;
            }
        }

        static int[] turns = new int[]{
            1,2,1,2,    3,4,4,3,
            1,2,1,2,    4,3,4,3,
            2,1,        4,3
        };

        List<ListBox> bpGoals;

        String state = "";//"", "p1", "p2"
        String playerName;
        int turn = -1000;
        ListBox bpGoal;

        private void createGame_Click(object sender, RoutedEventArgs e)
        {
            playerName = name.Text;
            String content = "creategame:name=" + playerName;
            doSend(content);
        }

        private void joinGame_Click(object sender, RoutedEventArgs e)
        {
            playerName = name.Text;
            String gameName = (String)gamesBox.SelectedValue;
            String content = "joingame:game=" + gameName + ",name=" + playerName;
            doSend(content);
        }

        private void bpSubmit_Click(object sender, RoutedEventArgs e)
        {
            String heroName = heroText.Text;
            String content = "bp:hero=" + heroName;
            doSend(content);

            heroText.Text = "";
        }

        private void quitButton_Click(object sender, RoutedEventArgs e)
        {
            doSend("quit");
        }

        private void setStateMsg() {
            String stt = "空闲";
            if (state == "p1") {
                stt = "先ban先选";
            } else if(state == "p2"){
                stt = "后ban后选";
            }
            String msg = turn == -1000 ? "空闲" : (turn == -1 ? "已建立BP等待其他玩家加入" : "BP进行中，");
            if (turn >= 0) {
                msg += stt;
            }
            stateMsg.Content = msg;
            stateMsg.Background = Brushes.Wheat;

            bool empty = (stt == "空闲");
            name.IsEnabled = empty;
            createGame.IsEnabled = empty;
            joinGame.IsEnabled = empty;

            bool myTurn = false;
            Brush bpBrush = Brushes.White;
            if (turn >= 0 && turn < 20)
            {//bp进行中
                int flag = turns[turn];
                bpGoal = bpGoals[flag - 1];

                bpBrush = (flag < 3 ? Brushes.Red : Brushes.YellowGreen);

                //设置活跃box背景颜色
                foreach (ListBox g in bpGoals)
                {
                    g.Background = Brushes.White;
                }

                //英雄名称输入对话框和按钮：可用or禁用
                myTurn = (state == "p1" && flag % 2 == 1) || (state == "p2" && flag % 2 == 0);//轮到我了
                if (myTurn)
                {
                    stateMsg.Content = "请" + (flag < 3 ? "禁用" : "选择") + "英雄";
                    stateMsg.Background = bpBrush;
                }
                else
                {
                    stateMsg.Content = "轮到敌方" + (flag < 3 ? "禁用" : "选择") + "英雄";
                    stateMsg.Background = Brushes.Wheat;
                }
                bpGoal.Background = myTurn ? bpBrush : Brushes.Wheat;
            }
            else if (turn >= 20)
            {
                stateMsg.Content = "已成功完成BP";
                stateMsg.Background = Brushes.Wheat;
            }
            //英雄名称输入对话框和按钮：可用or禁用
            bpSubmit.IsEnabled = myTurn;
            bpSubmit.Background = myTurn ? bpBrush : Brushes.White;
            heroText.IsEnabled = myTurn;
            heroText.Background = myTurn ? bpBrush : Brushes.White;
            
            quitButton.IsEnabled = turn >= -1;
        }

    }
}
