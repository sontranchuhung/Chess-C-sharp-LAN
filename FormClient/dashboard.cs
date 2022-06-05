using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FormClient
{
    public partial class dashboard : Form
    {
        public dashboard()
        {
            InitializeComponent();
        }
        public static bool localPlay = false;
        private void btnSinglePlay_Click(object sender, EventArgs e)
        {
            ChessPlay newChessGame = new ChessPlay();
            newChessGame.ShowDialog();
        }

        private void btnLan_Click(object sender, EventArgs e)
        {
            localPlay = true;
            ChessPlay newChessGame = new ChessPlay();
            newChessGame.ShowDialog();
        }

        private void btnWan_Click(object sender, EventArgs e)
        {

        }
    }
}
