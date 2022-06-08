﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Chess;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace FormClient
{
    public partial class ChessPlay : Form
    {
        static public Chess.ChessBoard chessBoard = new Chess.ChessBoard();
        Chess.Point selectedPiece = new Chess.Point();

        //Nếu chưa hiện nước có thể đi là -1, còn nếu thực hiện thay đổi bàn cờ thì là 0 hoặc 1 tuỳ vào quân cờ
        static int selectedPlayer = -1;

        #region Local play

        public Socket connectingSocket;
        public int player = -1;
        public bool isYourturn;
        public bool BeforeGameStage = true;
        public byte[] clientBuffer;
        //buffer = {0 clientPlayer, 1 Win,2 Castling,3 PromotionValue,4 fromX,5 fromY,6 toX,7 toY}
        //byte[] để gửi
        public byte[] buffer = new byte[8];
        //byte[] để nhận
        static public byte[] receiveBytes = new byte[8];

        public int clientPlayer = -1;
        public int Win = 0;
        public int Castling = 0;
        public int PromotionValue = -1;
        public int fromX = -1;
        public int fromY = -1;
        public int toX = -1;
        public int toY = -1;

        public bool localPlay = dashboard.localPlay;

        #endregion

        public ChessPlay()
        {
            InitializeComponent();
        }

        private void Form_Load(object sender, EventArgs e)
        {
            for (int x = 1; x < boardLayoutPanel.ColumnCount; x++)
            {
                for (int y = 1; y < boardLayoutPanel.RowCount; y++)
                {
                    #region TẠO BÀN CỜ VỚI MỖI Ô TRONG PANEL LÀ MỘT BUTTON

                    Button button = new Button();
                    button.Dock = DockStyle.Fill;
                    button.Margin = new Padding(0);
                    //button.FlatStyle = FlatStyle.Popup;
                    button.FlatAppearance.BorderSize = 0;

                    boardLayoutPanel.Controls.Add(button);
                    #endregion

                    //Mỗi khi tạo một button thì load lại bàn cờ
                    button.Click += Click_Board;

                }
            }

            if (localPlay)
            {
                TryToConnect();
            }

            DrawPieces(chessBoard);
        }//Vẽ bàn cờ 

        public void BufferOpen()
        {
            chessBoard.ActionPiece(receiveBytes[3], receiveBytes[4], receiveBytes[5], receiveBytes[6]);
            DrawPieces(chessBoard);
            chessBoard.SwapPlayerTurn();
        }

        private void Click_Board(object s, EventArgs e)
        {
            #region LOAD BÀN CỜ MỖI KHI CLICK BUTTON
            byte[] tempBuffer = (byte[])buffer.Clone();

            DrawPieces(chessBoard);
            if (!(s is Button)) return;

            if (localPlay == true && player != chessBoard.playerTurn)
            {
                MessageBox.Show("Chưa tới lượt bạn!");
                return;
            }

            Button button = (Button)s;
            button.BackColor = System.Drawing.Color.Transparent;
            button.FlatStyle = FlatStyle.Standard;
            button.UseVisualStyleBackColor = true;
            TableLayoutPanelCellPosition a = boardLayoutPanel.GetPositionFromControl((Control)s);

            #endregion

            #region BUTTON KHÔNG PHẢI QUÂN CỜ

            if (!(button.Tag is ChessPiece))
            {
                //nếu button là Nước nằm trong (AvailableMoves) mà quân cờ có thể đi
                if (selectedPlayer > -1)
                {
                    //Trường hợp có thể di chuyển nhưng không di chuyển hợp lệ thì reset lại
                    if (!chessBoard.ActionPiece(selectedPiece.x, selectedPiece.y, a.Column - 1, a.Row - 1))
                    {
                        MessageBox.Show("Đi sai luật chơi");
                        selectedPlayer = -1;
                        return;
                    }

                    if (localPlay == true)
                    {
                        Send(buffer);
                        buffer[3] = (byte)selectedPiece.x;
                        buffer[4] = (byte)selectedPiece.y;
                        buffer[5] = (byte)(a.Column - 1);
                        buffer[6] = (byte)(a.Row - 1);
                        Send(buffer);
                        buffer = tempBuffer;//reset lại buffer để gửi lần tiếp theo
                        StartReceiving();
                    }

                    DrawPieces(chessBoard);
                    chessBoard.SwapPlayerTurn();
                    selectedPlayer = -1;
                }
                //selectedPlayer = -1;
                return;
            }
            #endregion

            #region BUTTON LÀ QUÂN CỜ

            ChessPiece chessPiece = (ChessPiece)button.Tag;


            //Thực hiện ăn quân cờ (Đối với quân cờ của đối phương)
            if (selectedPlayer > -1 && selectedPlayer != chessPiece.Player)
            {
                //Trường hợp có thể ăn quân cờ nhưng không hợp lệ thì reset lại
                if (!chessBoard.ActionPiece(selectedPiece.x, selectedPiece.y, a.Column - 1, a.Row - 1))
                {
                    MessageBox.Show("Đi sai luật chơi");
                    selectedPlayer = -1;
                    return;
                }
                if (localPlay == true)
                {
                    Send(buffer);
                    buffer[3] = (byte)selectedPiece.x;
                    buffer[4] = (byte)selectedPiece.y;
                    buffer[5] = (byte)(a.Column - 1);
                    buffer[6] = (byte)(a.Row - 1);
                    Send(buffer);
                    buffer = tempBuffer;//reset lại buffer để gửi lần tiếp theo
                }
                DrawPieces(chessBoard);
                chessBoard.SwapPlayerTurn();
                selectedPlayer = -1;
            }

            //Hiện nước đi của quân mình
            else
            {
                selectedPlayer = chessPiece.Player;

                //Check lượt di chuyển quân khi chơi single play
                if (chessBoard.playerTurn != selectedPlayer)
                {
                    MessageBox.Show("Nhầm lượt đi!");
                    selectedPlayer = -1;
                    return;
                }
                selectedPiece.x = a.Column - 1;
                selectedPiece.y = a.Row - 1;
                foreach (Chess.Point point in chessBoard.PieceActions(a.Column - 1, a.Row - 1))
                {
                    Button actionButton = (Button)boardLayoutPanel.GetControlFromPosition(point.x + 1, point.y + 1);
                    if (actionButton.BackgroundImage == null)
                    {
                        actionButton.BackgroundImage = global::Chess.Properties.Resources.dot;
                        actionButton.BackgroundImageLayout = ImageLayout.Stretch;
                    }
                    else actionButton.BackColor = Color.LightSkyBlue;
                }
                if (chessBoard.KingInCheck(chessPiece.Player) && chessBoard.PieceActions(a.Column - 1, a.Row - 1).Count() == 0)
                    MessageBox.Show("Vua đang bị chiếu");
            }
            #endregion
        }

        public void DrawPieces(Chess.ChessBoard board)
        {
            for (int x = 0; x < board.GetLength(0); x++)
            {
                for (int y = 0; y < board.GetLength(1); y++)
                {
                    Button button = (Button)boardLayoutPanel.GetControlFromPosition(x + 1, y + 1);


                    #region NẾU BUTTON LÀ MỘT CHESSPIECE THÌ GẮN TAG VÀO BUTTON

                    if (board[x, y] != null)
                    {
                        ChessPiece chessPiece = board[x, y];

                        button.Tag = chessPiece;
                        button.Text = "";

                        if (chessPiece.Player == 1)
                        {
                            switch (chessPiece.ToString().Replace("Chess.", ""))
                            {
                                case ("Knight"):
                                    button.BackgroundImage = global::Chess.Properties.Resources.w_knight;
                                    break;
                                case ("Bishop"):
                                    button.BackgroundImage = global::Chess.Properties.Resources.w_bishop;
                                    break;
                                case ("Rook"):
                                    button.BackgroundImage = global::Chess.Properties.Resources.w_rook;
                                    break;
                                case ("Queen"):
                                    button.BackgroundImage = global::Chess.Properties.Resources.w_queen;
                                    break;
                                case ("Pawn"):
                                    button.BackgroundImage = global::Chess.Properties.Resources.w_pawn;
                                    break;
                                case ("King"):
                                    button.BackgroundImage = global::Chess.Properties.Resources.w_king;
                                    break;
                            }
                        }
                        else
                        {
                            switch (chessPiece.ToString().Replace("Chess.", ""))
                            {
                                case ("Knight"):
                                    button.BackgroundImage = global::Chess.Properties.Resources.b_knight;
                                    break;
                                case ("Bishop"):
                                    button.BackgroundImage = global::Chess.Properties.Resources.b_bishop;
                                    break;
                                case ("Rook"):
                                    button.BackgroundImage = global::Chess.Properties.Resources.b_rook;
                                    break;
                                case ("Queen"):
                                    button.BackgroundImage = global::Chess.Properties.Resources.b_queen;
                                    break;
                                case ("Pawn"):
                                    button.BackgroundImage = global::Chess.Properties.Resources.b_pawn;
                                    break;
                                case ("King"):
                                    button.BackgroundImage = global::Chess.Properties.Resources.b_king;
                                    break;
                            }
                        }

                        button.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
                        button.FlatStyle = FlatStyle.Popup;
                        button.UseVisualStyleBackColor = false;
                    }
                    #endregion

                    #region NẾU BUTTON KHÔNG PHẢI LÀ MỘT CHESSPIECE

                    else
                    {
                        button.BackgroundImage = null;
                        button.FlatStyle = FlatStyle.Popup;
                        button.Text = "";
                        button.Tag = null;
                    }

                    #endregion

                    if ((x + y) % 2 == 1) button.BackColor = Color.RosyBrown;
                    else button.BackColor = Color.White;

                    this.coordinates.SetToolTip(button, String.Format("({0}, {1})", x, y));
                }
            }

            #region KIỂM TRA CHIẾU HẾT, PHONG CẤP, NHẬP THÀNH



            //Chiếu hết
            //if (chessBoard.KingInCheck(chessBoard.playerTurn))
            //{
            //    bool GameMaybeEnd = true;
            //    //Duyệt mảng, nếu vua đang bị chiếu nhưng không có quân cờ nào có khả năng di chuyển thì nó là chiếu bí
            //    for (int x = 0; x < board.GetLength(0); x++)
            //    {
            //        for (int y = 0; y < board.GetLength(1); y++)
            //        {
            //            if (chessBoard[x, y] != null)
            //                if (chessBoard[x, y].Player == chessBoard.playerTurn && chessBoard.PieceActions(x, y).Count() != 0)
            //                {
            //                    GameMaybeEnd = false;
            //                    Win = 1;
            //                    break;
            //                }
            //        }
            //        if (!GameMaybeEnd)
            //        {
            //            MessageBox.Show("Bạn đã bị chiếu hết");
            //            this.Close();
            //            break;
            //        }
            //    }
            //}
            ////Phong cấp quân
            //if (chessBoard.PromotionValue != -1)
            //{
            //    PromotionValue = chessBoard.PromotionValue;
            //    chessBoard.PromotionValue = -1;
            //}
            ////Nhập thành
            //if (chessBoard.castling == 1)
            //{
            //    Castling = 1;
            //    chessBoard.castling = 0;
            //}

            

            #endregion
        }

        //socket things
        public void TryToConnect()
        {
            connectingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            while (!connectingSocket.Connected)
            {
                Thread.Sleep(1000);

                try
                {
                    connectingSocket.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234));
                }
                catch { MessageBox.Show("Connect to server failed!"); }
            }
            StartReceiving();
        }


        public void Send(byte[] buffer)
        {
            try
            {
                if (player == 1 && buffer[3] != 0)
                {
                    MessageBox.Show("client 1 sent buffer");
                }
                else if (player == 0 && buffer[3] != 0)
                    MessageBox.Show("client 2 sent buffer");
                else
                    MessageBox.Show("Send trash message");
                connectingSocket.Send(buffer);
            }
            catch (Exception ex)
            {
                throw new Exception();
            }
        }

        public void StartReceiving()
        {
            try
            {
                clientBuffer = new byte[8];
                connectingSocket.BeginReceive(clientBuffer, 0, clientBuffer.Length, SocketFlags.None, ReceiveCallback, null);

            }
            catch { }
        }
        public void ReceiveCallback(IAsyncResult AR)
        {
            bool FirstCallback = true;
            try
            {
                if (connectingSocket.EndReceive(AR) > 1)
                {
                    connectingSocket.Receive(clientBuffer, clientBuffer.Length, SocketFlags.None);

                    if (player == 1)
                        MessageBox.Show($"Client 1 receive \r\n" +
                            $"byte[0]:{clientBuffer[0]} \r\n" +
                        $"byte[3]:{clientBuffer[3]}\r\n" +
                        $"byte[4]:{clientBuffer[4]}\r\n");
                    else if (player == 0)
                        MessageBox.Show($"Client 2 receive \r\n" +
                            $"byte[0]:{clientBuffer[0]} \r\n" +
                        $"byte[3]:{clientBuffer[3]}\r\n" +
                        $"byte[4]:{clientBuffer[4]}\r\n");

                    if (!BeforeGameStage && player == -1)
                    {
                        MessageBox.Show("Error code 02");//02
                    }


                    //Được gọi bởi hàm TryToConnect
                    if (BeforeGameStage)
                    {
                        BeforeGameStage = false;
                        //Server quyết định chọn bên trắng hay đen cho client
                        if (clientBuffer[0] == 1)
                        {
                            player = 1;
                            MessageBox.Show("Bạn là bên trắng, bạn đi trước");
                        }
                        else if (clientBuffer[0] == 0)
                        {
                            player = 0;
                            MessageBox.Show("Bạn là bên đen, đợi nước đi");
                            StartReceiving();
                        }
                        else
                            MessageBox.Show("Error code 01");//01
                    }
                    else
                    {
                        if (player == 0 && FirstCallback)
                        {
                            Send(clientBuffer);
                            Send(clientBuffer);
                            MessageBox.Show($"client 2 send buffer{clientBuffer[3]}");
                            FirstCallback = !FirstCallback;
                        }

                        //Truyền bytes vừa nhận vào receivebytes để xử lý ở class ChessPlay
                        receiveBytes = clientBuffer;

                        clientBuffer = new byte[8];
                        BufferOpen();
                    }
                }
                else
                {
                    connectingSocket.Disconnect(true);
                }
            }
            catch
            {
                //socket exception, Gỡ kết nối và cố kết nối lại với server
                if (!connectingSocket.Connected)
                {
                    connectingSocket.Disconnect(true);
                    TryToConnect();
                }
                else
                {
                    //StartReceiving();
                }
            }
        }






    }
}
