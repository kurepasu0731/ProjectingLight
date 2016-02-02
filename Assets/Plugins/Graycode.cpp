#include <opencv2/opencv.hpp>
#include <iomanip>


extern "C" {


	__declspec(dllexport) void makeGraycodeImage(int proWidth, int proHeight)
    {
		std::vector<std::vector<int>> graycode;
		graycode.resize(proHeight);
		for(int i = 0; i < proHeight; ++i)
		{
			graycode[i].resize(proWidth);
		}
		unsigned int h_bit = (int)ceil( log(proHeight+1) / log(2) );
		unsigned int w_bit = (int)ceil( log(proWidth+1) / log(2) );
		unsigned int all_bit = h_bit + w_bit;

		int *bin_code_h = new int[proHeight];  // 2進コード（縦）
		int *bin_code_w = new int[proWidth];   // 2進コード（横）
		int *graycode_h = new int[proHeight];  // グレイコード（縦）
		int *graycode_w = new int[proWidth];   // グレイコード（横）
		

		/***** 2進コード作成 *****/
		// 行について
		for( int y = 0; y < proHeight; y++ )
			bin_code_h[y] = y + 1;
		// 列について
		for( int x = 0; x < proWidth; x++ )
			bin_code_w[x] = x + 1;

		/***** グレイコード作成 *****/
		// 行について
		for( int y = 0; y < proHeight; y++ )
			graycode_h[y] = bin_code_h[y] ^ ( bin_code_h[y] >> 1 );
		// 列について
		for( int x = 0; x < proWidth; x++ )
			graycode_w[x] = bin_code_w[x] ^ ( bin_code_w[x] >> 1 );
		// 行列を合わせる（行 + 列）
		for( int y = 0; y < proHeight; y++ ) {
			for( int x = 0; x < proWidth; x++ )
				graycode[y][x] = ( graycode_h[y] << w_bit) | graycode_w[x];
		}

		delete[] bin_code_h;
		delete[] bin_code_w;
		delete[] graycode_h;
		delete[] graycode_w;


        cv::Mat posi_img ( proHeight, proWidth, CV_8UC3, cv::Scalar(0, 0, 0) );
		cv::Mat nega_img ( proHeight, proWidth, CV_8UC3, cv::Scalar(0, 0, 0) );
		int bit = all_bit-1;
		std::stringstream *Filename_posi = new std::stringstream[all_bit];  // 書式付入出力
		std::stringstream *Filename_nega = new std::stringstream[all_bit];  // 書式付入出力

		// ポジパターンコード画像作成
		for( unsigned int z = 0; z < all_bit; z++) {
			for( int y = 0; y < proHeight; y++ ) {
				for( int x = 0; x < proWidth; x++ ) {
					if( ( (graycode[y][x] >> (bit-z)) & 1 ) == 0 ) {  // 最上位ビットから順に抽出し，そのビットが0だった時
						posi_img.at<cv::Vec3b>( y, x )[0] = 0;  // B
						posi_img.at<cv::Vec3b>( y, x )[1] = 0;  // G
						posi_img.at<cv::Vec3b>( y, x )[2] = 0;  // R
					}else if( ( (graycode[y][x] >> (bit-z)) & 1 ) == 1 ) {
						posi_img.at<cv::Vec3b>( y, x )[0] = 255;  // B
						posi_img.at<cv::Vec3b>( y, x )[1] = 255;  // G
						posi_img.at<cv::Vec3b>( y, x )[2] = 255;  // R
					}
				}
			}
			// 連番でファイル名を保存（文字列ストリーム）
			Filename_posi[z] << "Calibration/GrayCodeImage/ProjectionGrayCode/posi" << std::setw(2) << std::setfill('0') << z << ".bmp"; 
			cv::imwrite(Filename_posi[z].str(), posi_img);
			Filename_posi[z] << std::endl;
		}

		// ネガパターンコード画像作成
		for( unsigned int z = 0; z < all_bit; z++) {
			for( int y = 0; y < proHeight; y++ ) {
				for( int x = 0; x < proWidth; x++ ) {
					if( ( (graycode[y][x] >> (bit-z)) & 1 ) == 1 ) {
						nega_img.at<cv::Vec3b>( y, x )[0] = 0;  // B
						nega_img.at<cv::Vec3b>( y, x )[1] = 0;  // G
						nega_img.at<cv::Vec3b>( y, x )[2] = 0;  // R
					}else if( ( (graycode[y][x] >> (bit-z)) & 1 ) == 0 ) {
						nega_img.at<cv::Vec3b>( y, x )[0] = 255;  // B
						nega_img.at<cv::Vec3b>( y, x )[1] = 255;  // G
						nega_img.at<cv::Vec3b>( y, x )[2] = 255;  // R
					}
				}
			}
			// 連番でファイル名を保存（文字列ストリーム）
			Filename_nega[z] << "Calibration/GrayCodeImage/ProjectionGrayCode/nega" << std::setw(2) << std::setfill('0') << z << ".bmp"; 
			cv::imwrite(Filename_nega[z].str(), nega_img);
			Filename_nega[z] << std::endl;
		}

		delete[] Filename_posi;
		delete[] Filename_nega;
    }


	// カメラ撮影画像を読み込む関数
	void loadCam(cv::Mat &mat, int div_bin, bool vh, bool pn)
	{
		char buf[256];
		sprintf_s(buf, "Calibration/GrayCodeImage/CaptureImage/CameraImg%d_%02d_%d.bmp", vh, div_bin, pn);
		mat = cv::imread(buf, 0);
	}


	// グレイコードの画像を利用してマスクを生成する関数
	// ポジとネガの差分を取ってthresholdValue以上の輝度のピクセルを白にする
	void makeMaskFromCam(cv::Mat &posi, cv::Mat &nega, cv::Mat &result, int thresholdValue, int camWidth, int camHeight)
	{
		result = cv::Mat::zeros(cv::Size(camWidth,camHeight), CV_8UC1);

		for(int j=0; j<camHeight; j++){
			for(int i=0; i<camWidth; i++){
				int posi_i = posi.at<uchar>(j, i);
				int nega_i = nega.at<uchar>(j, i);

				if (abs(posi_i - nega_i) > thresholdValue){
					result.at<uchar>(j, i) = 255;
				}else{
					result.at<uchar>(j, i) = 0;
				}
			}
		}
	}

	// マスクを作成するインタフェース
	void makeMask(cv::Mat &mask, int camWidth, int camHeight)
	{
		cv::Mat posi_img;
		cv::Mat nega_img;

		// マスク画像生成
		cv::Mat mask_vert, mask_hor;
		static int useImageNumber = 6;
		// y方向のグレイコード画像読み込み
		loadCam(posi_img, useImageNumber, 0, 1);
		loadCam(nega_img, useImageNumber, 0, 0);

		// 仮のマスク画像Y生成
		makeMaskFromCam(posi_img, nega_img, mask_vert, 25, camWidth, camHeight);

		// x方向のグレイコード画像読み込み
		loadCam(posi_img, useImageNumber, 1, 1);
		loadCam(nega_img, useImageNumber, 1, 0);

		// 仮のマスク画像X生成
		makeMaskFromCam(posi_img, nega_img, mask_hor, 25, camWidth, camHeight);

		// XとYのORを取る
		// マスク外はどちらも黒なので黒
		// マスク内は（理論的には）必ず一方が白でもう一方が黒なので、白になる
		// 実際はごま塩ノイズが残ってしまう
		cv::bitwise_or(mask_vert, mask_hor, mask);

		// 残ったごま塩ノイズを除去（白ゴマか黒ゴマかで適用順が逆になる）
		dilate(mask, mask, cv::Mat(), cv::Point(-1, -1), 5);
		erode(mask, mask, cv::Mat(), cv::Point(-1, -1), 5);

		cv::imwrite("Calibration/GrayCodeImage/mask.bmp", mask);
	}

	// 実際の2値化処理 
	void thresh( cv::Mat &posi, cv::Mat &nega, cv::Mat &thresh_img, int thresh_value )
	{
		thresh_img = cv::Mat(posi.rows, posi.cols, CV_8UC1);
		for( int y = 0; y < posi.rows; y++ ) {
			for(int x = 0; x < posi.cols; x++ ) {
				int posi_pixel = posi.at<uchar>( y, x );
				int nega_pixel = nega.at<uchar>( y, x );

				// thresh_valueより大きいかどうかで二値化
				if( posi_pixel - nega_pixel >= thresh_value )
					thresh_img.at<uchar>( y, x ) = 255;
				else
					thresh_img.at<uchar>( y, x ) = 0;
			}
		}
	}


	// 撮影画像の2値化をするインタフェース
	__declspec(dllexport) void make_thresh(int proWidth, int proHeight, int camWidth, int camHeight)
	{
		cv::Mat posi_img;
		cv::Mat nega_img;
		cv::Mat Geometric_thresh_img;  // 2値化された画像
		cv::Mat mask;

		// マスクを生成
		makeMask(mask, camWidth, camHeight);

		int h_bit = (int)ceil( log(proHeight+1) / log(2) );
		int w_bit = (int)ceil( log(proWidth+1) / log(2) );
		int all_bit = h_bit + w_bit;

		// 連番でファイル名を読み込む
		for( int i = 0; i < h_bit; i++ ) {
			// 読み込み
			char buf[256];
			// ポジパターン読み込み
			loadCam(posi_img, i+1, 0, 1);
			// ネガパターン読み込み
			loadCam(nega_img, i+1, 0, 0);

			// 2値化
			cv::Mat masked_img;
			thresh( posi_img, nega_img, Geometric_thresh_img, 0 );
			// マスクを適用して2値化
			Geometric_thresh_img.copyTo( masked_img, mask );
			sprintf_s(buf, "Calibration/GrayCodeImage/ThresholdImage/Geometric_thresh%02d.bmp", i);
			cv::imwrite(buf, masked_img);
		}
		for( int i = 0; i < w_bit; i++ ) {
			// 読み込み
			char buf[256];
			// ポジパターン読み込み
			loadCam(posi_img, i+1, 1, 1);
			// ネガパターン読み込み
			loadCam(nega_img, i+1, 1, 0);

			// 2値化
			cv::Mat masked_img;
			thresh( posi_img, nega_img, Geometric_thresh_img, 0 );
			// マスクを適用して2値化
			Geometric_thresh_img.copyTo( masked_img, mask );
			sprintf_s(buf, "Calibration/GrayCodeImage/ThresholdImage/Geometric_thresh%02d.bmp", i+h_bit);
			cv::imwrite(buf, masked_img);
		}
	}


	// 対応付けを行うインターフェース
	__declspec(dllexport) void makeCorrespondence(int proWidth, int proHeight, int camWidth, int camHeight, int proPointx[], int proPointy[], int camPointx[], int camPointy[], int& correspondNum)
	{
		std::vector<std::vector<int>> prograycode;
		prograycode.resize(proHeight);
		for(int i = 0; i < proHeight; ++i)
		{
			prograycode[i].resize(proWidth);
		}
		unsigned int h_bit = (int)ceil( log(proHeight+1) / log(2) );
		unsigned int w_bit = (int)ceil( log(proWidth+1) / log(2) );
		unsigned int all_bit = h_bit + w_bit;

		int *bin_code_h = new int[proHeight];  // 2進コード（縦）
		int *bin_code_w = new int[proWidth];   // 2進コード（横）
		int *graycode_h = new int[proHeight];  // グレイコード（縦）
		int *graycode_w = new int[proWidth];   // グレイコード（横）
		

		/***** 2進コード作成 *****/
		// 行について
		for( int y = 0; y < proHeight; y++ )
			bin_code_h[y] = y + 1;
		// 列について
		for( int x = 0; x < proWidth; x++ )
			bin_code_w[x] = x + 1;

		/***** グレイコード作成 *****/
		// 行について
		for( int y = 0; y < proHeight; y++ )
			graycode_h[y] = bin_code_h[y] ^ ( bin_code_h[y] >> 1 );
		// 列について
		for( int x = 0; x < proWidth; x++ )
			graycode_w[x] = bin_code_w[x] ^ ( bin_code_w[x] >> 1 );
		// 行列を合わせる（行 + 列）
		for( int y = 0; y < proHeight; y++ ) {
			for( int x = 0; x < proWidth; x++ )
				prograycode[y][x] = ( graycode_h[y] << w_bit) | graycode_w[x];
		}

		delete[] bin_code_h;
		delete[] bin_code_w;
		delete[] graycode_h;
		delete[] graycode_w;



		std::vector<std::vector<int>> graycode;
		graycode.resize(camHeight);
		for(int i = 0; i < camHeight; ++i)
		{
			graycode[i].resize(camWidth);
		}
		for( int y = 0; y < camHeight; y++ ) {
			for( int x = 0; x < camWidth; x++ ){
				graycode[y][x] = 0;
			}
		}

		
		// 2値化コード復元
		
		for( unsigned int i = 0; i <all_bit; i++ ) {
			char buf[256];
			sprintf_s(buf, "Calibration/GrayCodeImage/ThresholdImage/Geometric_thresh%02d.bmp", i);
			cv::Mat a = cv::imread(buf, 0);

			for( int y = 0; y < camHeight; y++ ) {
				for( int x = 0; x < camWidth; x++ ) {
					if( a.at<uchar>( y, x ) == 255)
						graycode[y][x] = ( 1 << (all_bit-i-1) ) | graycode[y][x]; 
				}
			}
		}

		std::map<int, cv::Point> code_map;
		std::vector<std::vector<cv::Point>> CamPro;
		CamPro.resize(proHeight);
		for(int i = 0; i < proHeight; ++i)
		{
			CamPro[i].resize(proWidth);
		}


		// 連想配列でグレイコードの値の場所に座標を格納
		for( int y = 0; y < camHeight; y++ ) {
			for( int x = 0; x < camWidth; x++ ) {
				int a = graycode[y][x];
				if( a != 0 )
					code_map[a] = cv::Point(x, y);
			}
		}

		// 0番目は使わない
		code_map[0] = cv::Point(-1, -1);
		correspondNum=0;
		std::vector<int> proX;
		std::vector<int> proY;
		std::vector<int> camX;
		std::vector<int> camY;

		// プロジェクタとカメラの対応付け
		for( int y = 0; y < proHeight; y++ ) {
			for( int x = 0; x < proWidth; x++ ) {
				// グレイコード取得
				int a = prograycode[y][x];
				// map内に存在しないコード（カメラで撮影が上手くいかなかった部分）の場所にはエラー値-1を格納
				if ( code_map.find(a) == code_map.end() ) {
					CamPro[y][x] = cv::Point(-1, -1);
				}
				// 存在する場合は、対応するグレイコードの座標を格納
				else {
					CamPro[y][x] = code_map[a];

					if(code_map[a].x != -1 && code_map[a].y != -1)
					{
						proX.push_back((int)x);
						proY.push_back((int)y);
						camX.push_back((int)(code_map[a].x));
						camY.push_back((int)(code_map[a].y));
						correspondNum++;
					}
				}
			}
		}

		cv::vector<cv::Point> pro;
		cv::vector<cv::Point> cam;
		for(int i = 0; i < correspondNum; ++i)
		{
			proPointx[i] = proX.at(i);
			proPointy[i] = proY.at(i);
			camPointx[i] = camX.at(i);
			camPointy[i] = camY.at(i);
			cv::Point pro2(proX.at(i), proY.at(i));
			cv::Point cam2(camX.at(i), camY.at(i));
			pro.push_back(pro2);
			cam.push_back(cam2);
		}

		cv::FileStorage fs2("Calibration/correspond.xml", cv::FileStorage::WRITE);
		cv::write(fs2,"pro", pro);
		cv::write(fs2,"cam", cam);
	}

}