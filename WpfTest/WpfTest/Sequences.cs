using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfTest {
	namespace NIDaq {
		//複数のシーケンス管理
		public class Sequences {
			//シーケンスリスト
			private List<Sequence> sequences = new List<Sequence>();
			//現在のシーケンスのインデックス
			private int currentId;

			//コンストラクタ
			public Sequences() {
				sequences.Add(new Sequence());
				currentId = 0;
			}
			//現在のシーケンスを取得
			public Sequence getCurrentSequence() { return sequences[currentId]; }

			//表示するシーケンス変更
			public void changeActiveSequence(int index, Grid grid) {
				currentId = index;
				sequences[currentId].bindGridUI(grid);
			}

			//シーケンスを保存
			public string writeCurrentSeq() {
				return sequences[currentId].toSeq();
			}
			//シーケンスに読み込み
			public void loadCurrentSeq(string text) {
				Sequence nseq = new Sequence();
				try {
					nseq.fromSeq(text);
					sequences[currentId] = nseq;
				} catch (Exception e) {
					throw e;
				}
			}
			//新しいシーケンスを追加
			public void createNewSequence() {
				sequences.Add(new Sequence());
			}
		}
	}
}
