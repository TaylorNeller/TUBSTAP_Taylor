using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Text;

namespace SimpleWars {
	class VSServer {
		private TcpListener fListener;
		private Thread fServerThread;
		private ClientTcpIp[] fClients;
		public int fStatus = STATUS_DISABLED;
		public int fLogcolor = 0;

		public const int STATUS_DISABLED = 0;
		public const int STATUS_LISTEN = 1;
		public const int STATUS_READY = 2;


		// 対戦の受付を開始
		public void listenStart() {
			// IPアドレス＆ポート番号設定
			int myPort = Settings.ConnectionPort;
			//            IPAddress myIp = IPAddress.Parse(Settings.ConnectionIP); 
			IPAddress myIp = null;
			foreach (IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName())) {
				if (ip.AddressFamily == AddressFamily.InterNetwork) myIp = ip;
			}

            //IPアドレスを自分で指定する場合
            if (Settings.useServerIP) myIp = IPAddress.Parse(Settings.ConnectionIP);

			IPEndPoint myEndPoint = new IPEndPoint(myIp, myPort);
			Logger.log(myIp.ToString() + " listen at port " + myPort + "\r\n", fLogcolor);


			// クライアント情報初期化
			fClients = new ClientTcpIp[2];

			// リスナー開始
			fListener = new TcpListener(myEndPoint);
			fListener.Start();

			// クライアント接続待ち開始
			fServerThread = new Thread(new ThreadStart(ServerThread));
			fServerThread.Start();

			fStatus = STATUS_LISTEN;
		}

		// 状態を聞いて文字列で格納
		public void askStatus(String[] currentStatus) {
			sendCommand(0, "status");
			sendCommand(1, "status");
			String s0 = waitForResponse(0);
			String s1 = waitForResponse(1);

			String[] s0split = s0.Split(' ');
			String[] s1split = s1.Split(' ');

			if (s0split[0].Equals(s1split[1]) == false || s1split[0].Equals(s0split[1]) == false) {
				Logger.log("VSServer.askStatus mismatch" + "\r\n", fLogcolor);
			}
			currentStatus[0] = s0split[0];
			currentStatus[1] = s0split[1];
		}

		// gameoverを流して応答を受け取る
		public void sendGameOver() {
			for (int idx = 0; idx < 2; idx++) sendCommand(idx, "gameover");
			for (int idx = 0; idx < 2; idx++) waitForResponse(idx);
		}


		// 名前を尋ねて受け取る
		public String askPlayerName(int idx) {
			sendCommand(idx, "name");
			return waitForResponse(idx);
		}

		// 相手の名前を伝える
		public void sendOpponentName(int idx, String name) {
			sendCommand(idx, "opponent " + name);
			waitForResponse(idx);
		}

		// 使用するマップ，それぞれの色，など対戦に必要な条件を送りつける
		public void sendRules(Map aMAP) {
			for (int idx = 0; idx < 2; idx++) {
				sendCommand(idx, "MAP " + aMAP.sendMapFile());
				waitForResponse(idx);  // ほんとは wait は非同期でまとめてでもいい

				sendCommand(idx, "Color " + idx); // 最初に入ってきた方が赤軍，後から入ってきた方が青軍
				waitForResponse(idx);
			}
		}

		// 着手を尋ねる．ついでに相手側に送る
		public Action askAction(int idx,Action aAct) {
			sendCommand(idx, "Genmove");
			string s = waitForResponse(idx);
			aAct = Action.Parse(s);

			sendCommand((idx + 1) % 2,"play "+ s);
			waitForResponse((idx + 1) % 2);

			return aAct;
		}

		#region 通信部

		// クライアント接続待ちスレッド
		private void ServerThread() {
			try {
				while (true) {
					// ソケット接続待ち
					TcpClient myTcpClient = fListener.AcceptTcpClient();

					Logger.log("player joined" + "\r\n", fLogcolor);

					// クライアントから接続有り．空いてるところを探す
					int idx = -1;
					for (int i = 1; i >= 0; i--) {
						if (fClients[i] == null) {
							idx = i;
						} else if (fClients[i].objSck.Connected == false) {
							idx = i;
						}
					}

					if (idx == -1) {
						myTcpClient.Close();  //空きがない （ここにくることは稀なはずだが一応）
					} else {
						// クライアント送受信オブジェクト生成
						fClients[idx] = new ClientTcpIp();
						fClients[idx].index = idx;
						fClients[idx].fStringReceived = "";
						fClients[idx].objSck = myTcpClient;
						fClients[idx].objStm = myTcpClient.GetStream();
						// クライアントとの送受信開始
						Thread myClientThread = new Thread(new ThreadStart(fClients[idx].ReadWrite));
						myClientThread.Start();

						// 埋まった
						if (idx == 1) {
							fStatus = STATUS_READY;
							break;
						}
					}
				}
			} catch {
				// fListener がStopされると多分 AcceptTcpClient() が例外を飛ばしてここにきてスレッドは終わるはず
				//System.Windows.Forms.MessageBox.Show("ServerThread end");
			}
		}




		// 受付を終了
		public void listenEnd() {
			fListener.Stop();
			fStatus = STATUS_DISABLED;
		}

		// クライアント切断
		public void closeConnection() {
			listenEnd();
			for (int i = 0; i < fClients.Length; i++) {
				if (fClients[i] != null) {
					if (fClients[i].objSck.Connected == true) {
						fClients[i].objStm.Close();
						fClients[i].objSck.Close();
					}
				}
			}
		}


		// command...\r\n の形でメッセージする
		private void sendCommand(int idx, String message) {
			Logger.log("send to " + idx + " : " + message + "\r\n", fLogcolor);
			Byte[] dat = System.Text.Encoding.GetEncoding("UTF-8").GetBytes(message + "\r\n");
			fClients[idx].objStm.Write(dat, 0, dat.GetLength(0));
		}

		// = ...\r\n\r\n の形のものが来るまで待ち，...の部分を返す．
		// 複数のコマンドがちぎれたりひっついたりするので， \r\n\r\n の後ろを次の waitFor のために残す
		private String waitForResponse(int idx) {
			while (true) {
				Thread.Sleep(50);
				Application.DoEvents();
				if (Signal.FormClosed) return "";

				String s = fClients[idx].fStringReceived;
				if (s.Length >= 6) {
					if (s.Substring(0, 2).Equals("= ")) {
						int pos = s.IndexOf("\r\n\r\n");
						if (pos > 0) {
							fClients[idx].fStringReceived = s.Substring(pos + 4); // \r\n\r\n の後ろから全てを残す // Todo ロック
							Logger.log("message from " + idx + " : " + s.Substring(2, pos - 2) + "\r\n", fLogcolor);
							return s.Substring(2, pos - 2);  // 返すのは，commandの後ろから \r\n\r\n の前まで
						}
					}
				}
			}
		}

		#endregion
	}

	// クライアント受信クラス
	// ほぼこれのコピー：　http://homepage2.nifty.com/nonnon/SoftSample/CS.NET/SampleTcpIpSvr.html
	public class ClientTcpIp {
		public int index;
		public TcpClient objSck;
		public NetworkStream objStm;
		public String fStringReceived;  // 受け取った文字列を溜めておく

		// クライアント送受信スレッド
		public void ReadWrite() {
			try {
				while (true) {
					// ソケット受信
					Byte[] rdat = new Byte[1024];
					int ldat = objStm.Read(rdat, 0, rdat.GetLength(0));
					if (ldat > 0) {
						// クライアントからの受信データ有り
						Byte[] sdat = new Byte[ldat];
						Array.Copy(rdat, sdat, ldat);
						String msg = System.Text.Encoding.GetEncoding("UTF-8").GetString(sdat);
						fStringReceived = fStringReceived + msg;  // 本当はこのへんロックしないといけないんじゃ
						//MessageBox.Show("from client " + msg);
					} else {
						// ソケット切断有り
						// ソケットクローズ
						//MessageBox.Show("server; closing detected " + index);
						objStm.Close();
						objSck.Close();
						return;
					}
				}
			} catch {
			}
		}
	}

	class Signal {
		public static bool FormClosed = false;
	}
}
