#include <opencv2/opencv.hpp>
#include <time.h>

extern "C" {


	//元画像の特徴点と、再計算した特徴点の誤差を求める
	double inspection_error_value(cv::Mat& cameraMat, cv::vector<cv::Point3d>& P, cv::vector<cv::Point2d>& groundTruth)
	{
		if(cameraMat.cols != 4 || cameraMat.rows != 3){
			return 0.0;
		}
		cv::vector<cv::Point2d> p;
		for(int i=0; i<(int)P.size(); i++){
			double x = (cameraMat.at<double>(0,0)*P[i].x + cameraMat.at<double>(0,1)*P[i].y + cameraMat.at<double>(0,2)*P[i].z + cameraMat.at<double>(0,3))
						/ (cameraMat.at<double>(2,0)*P[i].x + cameraMat.at<double>(2,1)*P[i].y + cameraMat.at<double>(2,2)*P[i].z + cameraMat.at<double>(2,3));
			double y = (cameraMat.at<double>(1,0)*P[i].x + cameraMat.at<double>(1,1)*P[i].y + cameraMat.at<double>(1,2)*P[i].z + cameraMat.at<double>(1,3))
						/ (cameraMat.at<double>(2,0)*P[i].x + cameraMat.at<double>(2,1)*P[i].y + cameraMat.at<double>(2,2)*P[i].z + cameraMat.at<double>(2,3));
			p.push_back(cv::Point2d(x,y));
		}

		double sum = 0.0;
		for(int i=0; i<(int)p.size(); i++){
			double error = pow(pow(groundTruth[i].x-p[i].x,2.0)+pow(groundTruth[i].y-p[i].y,2.0),0.5);
			sum = sum + error;
		}

		return sum/(int)p.size();
	}

	// ある1点の再投影誤差
	double inspectionPoint_error_value(cv::Point2d& point2d, cv::Point3d& point3d,cv::Mat& persMat)
	{
		double x = (persMat.at<double>(0,0)*point3d.x + persMat.at<double>(0,1)*point3d.y + persMat.at<double>(0,2)*point3d.z + persMat.at<double>(0,3))
				/ (persMat.at<double>(2,0)*point3d.x + persMat.at<double>(2,1)*point3d.y + persMat.at<double>(2,2)*point3d.z + persMat.at<double>(2,3));

		double y = (persMat.at<double>(1,0)*point3d.x + persMat.at<double>(1,1)*point3d.y + persMat.at<double>(1,2)*point3d.z + persMat.at<double>(1,3))
				/ (persMat.at<double>(2,0)*point3d.x + persMat.at<double>(2,1)*point3d.y + persMat.at<double>(2,2)*point3d.z + persMat.at<double>(2,3));


		double squareX = (point2d.x-x) * (point2d.x-x);
		double squareY = (point2d.y-y) * (point2d.y-y);

		return sqrt(squareX + squareY);
	}

	//ランダムに6点を抽出
	void get_six_points(cv::vector<cv::Point2d>& calib_p, cv::vector<cv::Point3d>& calib_P, cv::vector<cv::Point2d>& src_p, cv::vector<cv::Point3d>& src_P)
	{
		int i=0;
		srand(time(NULL));    /* 乱数の初期化 */
		cv::Vector<int> exists;
		while(i <= 6){
			int maxValue = (int)src_p.size();
			int v = rand() % maxValue;
			bool e2=false;
			for(int s=0; s<i; s++){
				if(exists[s] == v) e2 = true; 
			}
			if(!e2){
				exists.push_back(v);
				calib_P.push_back(src_P[v]);
				calib_p.push_back(src_p[v]);
				i++;
			}
		}
	}


	bool six_points_algolism(cv::vector<cv::Point3d>& P, cv::vector<cv::Point2d>& p, cv::Mat& cameraMat){
		cameraMat = cv::Mat(3, 4, CV_64FC1, cv::Scalar::all(0));
		cv::Mat A(12,12,CV_64FC1, cv::Scalar::all(0));
		cv::Mat At(12,12,CV_64FC1, cv::Scalar::all(0));
		cv::Mat W(12,12,CV_64FC1, cv::Scalar::all(0));

		//行列Aの作成
		for(int i=0; i<6; i++){
			A.at<double>(2*i,0) = P[i].x;			A.at<double>(2*i,1) = P[i].y;		   A.at<double>(2*i,2) = P[i].z;
			A.at<double>(2*i,3) = 1.0;				A.at<double>(2*i,4) = 0.0;			   A.at<double>(2*i,5) = 0.0;
			A.at<double>(2*i,6) = 0.0;				A.at<double>(2*i,7) = 0.0;			   A.at<double>(2*i,8) = -1*P[i].x*p[i].x;
			A.at<double>(2*i,9) = -1*P[i].y*p[i].x; A.at<double>(2*i,10)=-1*P[i].z*p[i].x; A.at<double>(2*i,11) = -1*p[i].x;

			A.at<double>(2*i+1,0) = 0.0;			  A.at<double>(2*i+1,1) = 0.0;			   A.at<double>(2*i+1,2) = 0.0;
			A.at<double>(2*i+1,3) = 0.0;			  A.at<double>(2*i+1,4) = P[i].x;		   A.at<double>(2*i+1,5) =P[i].y;
			A.at<double>(2*i+1,6) =  P[i].z;		  A.at<double>(2*i+1,7) = 1.0;			   A.at<double>(2*i+1,8) = -1*P[i].x*p[i].y;
			A.at<double>(2*i+1,9) = -1*P[i].y*p[i].y; A.at<double>(2*i+1,10)=-1*P[i].z*p[i].y; A.at<double>(2*i+1,11) = -1*p[i].y;
		}
		transpose(A, At);
		W = At * A;

		//固有値・固有ベクトルを求める
		cv::Mat eigenValues, eigenVectors;
		cv::eigen(W, eigenValues, eigenVectors);
		//最小固有値に対応する固有ベクトルの取得(固有値は降順に格納されている)
		cv::Mat min_eigenVectors(1,12,CV_64FC1, cv::Scalar::all(0));
		for(int i=0; i<12; i++){
			min_eigenVectors.at<double>(0,i) = eigenVectors.at<double>(11,i);
		}

		//カメラMatrixの取得
		for(int i=0; i<12; i++){
			cameraMat.at<double>(i/4,i%4) = min_eigenVectors.at<double>(0,i);
		}

		/*std::cout << "matrix A" << std::endl << A << std::endl << std::endl;
		std::cout << "matrix At" << std::endl << At << std::endl << std::endl;
		std::cout << "matrix W" << std::endl << W << std::endl << std::endl;
		std::cout << "eigen Values of W" << std::endl << eigenValues << std::endl << std::endl;
		std::cout << "eigen vectors of W" << std::endl << eigenVectors << std::endl << std::endl;
		std::cout << "minimum eigen vectors of W" << std::endl << min_eigenVectors << std::endl << std::endl;*/
		return true;
	}

	//6 points algorithm
	void six_points_calibration(cv::vector<cv::Point3d>& points_3d, cv::vector<cv::Point2d>& points_2d, cv::Mat& dst){
		double min_ave=100000;

		for(int i=0; i<40; i++){
			cv::vector<cv::Point2d> calib_2d_points;
			cv::vector<cv::Point3d> calib_3d_points;
			cv::Mat pm;
			double ave;

			get_six_points(calib_2d_points, calib_3d_points, points_2d, points_3d);
			six_points_algolism(calib_3d_points, calib_2d_points, pm);
			ave = inspection_error_value(pm, points_3d, points_2d);
			if(ave < min_ave){
				dst = pm;
				min_ave = ave;
			}
		}
	}



	// 透視投影変換行列の推定
	void calcProjectionMatrix(cv::vector<cv::Point3d>& op, cv::vector<cv::Point2d>& ip, cv::Mat& dst)
	{
		cv::Mat A;
		A.create(cv::Size(12, op.size()*2), CV_64FC1);

		for (int i = 0, j = 0; i < op.size()*2; i+=2, ++j)
		{
			A.at<double>(i, 0) = 0.0;
			A.at<double>(i, 1) = 0.0;
			A.at<double>(i, 2) = 0.0;
			A.at<double>(i, 3) = 0.0;

			A.at<double>(i, 4) = -op[j].x;
			A.at<double>(i, 5) = -op[j].y;
			A.at<double>(i, 6) = -op[j].z;
			A.at<double>(i, 7) = -1.0;

			A.at<double>(i, 8) = ip[j].y*op[j].x;
			A.at<double>(i, 9) = ip[j].y*op[j].y;
			A.at<double>(i, 10) = ip[j].y*op[j].z;
			A.at<double>(i, 11) = ip[j].y;

			A.at<double>(i+1, 0) = op[j].x;
			A.at<double>(i+1, 1) = op[j].y;
			A.at<double>(i+1, 2) = op[j].z;
			A.at<double>(i+1, 3) = 1.0;

			A.at<double>(i+1, 4) = 0.0;
			A.at<double>(i+1, 5) = 0.0;
			A.at<double>(i+1, 6) = 0.0;
			A.at<double>(i+1, 7) = 0.0;

			A.at<double>(i+1, 8) = -ip[j].x*op[j].x;
			A.at<double>(i+1, 9) = -ip[j].x*op[j].y;
			A.at<double>(i+1, 10) = -ip[j].x*op[j].z;
			A.at<double>(i+1, 11) = -ip[j].x;
		}

		cv::Mat pvect;
		cv::SVD::solveZ(A, pvect);

		cv::Mat pm(3, 4, CV_64FC1);
		for (int i = 0; i < 12; i++)
		{
			pm.at<double>(i/4, i%4) = pvect.at<double>( i );
		}
		dst = pm;
	}


	// キャリブレーション
	__declspec(dllexport) void calcCalibration(	double srcPoint2dx[], double srcPoint2dy[], double srcPoint3dx[], double srcPoint3dy[], double srcPoint3dz[], int srcCorrespond, int proWidth, int proHeight, double projectionMatrix[], double externalMatrix[], double& reprojectionResult)
	{
		cv::Mat point2dx(1, srcCorrespond, CV_64F, srcPoint2dx);
		cv::Mat point2dy(1, srcCorrespond, CV_64F, srcPoint2dy);
		cv::Mat point3dx(1, srcCorrespond, CV_64F, srcPoint3dx);
		cv::Mat point3dy(1, srcCorrespond, CV_64F, srcPoint3dy);
		cv::Mat point3dz(1, srcCorrespond, CV_64F, srcPoint3dz);


		// 対応点
		cv::vector<cv::Point2d> imagePoints;
		cv::vector<cv::Point3d> worldPoints;

		for(int i = 0; i < srcCorrespond; ++i)
		{
			cv::Point2d point2d(point2dx.at<double>(i), point2dy.at<double>(i));
			cv::Point3d point3d(point3dx.at<double>(i), point3dy.at<double>(i), point3dz.at<double>(i));

			imagePoints.push_back(point2d);
			worldPoints.push_back(point3d);
		}


		/////////////////// 透視投影変換行列の推定 /////////////////////////////////

		cv::Mat perspectiveMat;		// 透視投影変換行列

		// 透視投影変換行列の推定
		calcProjectionMatrix(worldPoints, imagePoints, perspectiveMat);

		// 再投影誤差
		reprojectionResult = inspection_error_value(perspectiveMat, worldPoints, imagePoints);
		

		perspectiveMat = perspectiveMat/perspectiveMat.at<double>(11);

		cv::Mat cameraMat;
		cv::Mat rotation;
		cv::Mat translate;
		cv::Mat translate2 = cv::Mat::eye(3,1,CV_64F);
		cv::Mat worldTranslate = cv::Mat::eye(3,1,CV_64F);
		cv::decomposeProjectionMatrix(perspectiveMat, cameraMat, rotation, translate);		// 透視投影変換行列の分解→あやしい


		// OpenGL用内部パラメータ

		cameraMat = -cameraMat / cameraMat.at<double>(8);

		double far = 1000.0;
		double near = 0.05;
		
		cv::Mat cameraMatrix0 = cv::Mat::zeros(4, 4, CV_64F);
		cv::Mat cameraMatrix = cv::Mat::zeros(4, 4, CV_64F);


		cameraMatrix0.at<double>(0) = cameraMat.at<double>(0);
		cameraMatrix0.at<double>(1) = cameraMat.at<double>(1);
		cameraMatrix0.at<double>(2) = cameraMat.at<double>(2);
		cameraMatrix0.at<double>(5) = cameraMat.at<double>(4);
		cameraMatrix0.at<double>(6) = cameraMat.at<double>(5);
		cameraMatrix0.at<double>(10) = -(far+near) / (far-near);
		cameraMatrix0.at<double>(11) = -2*far*near / (far-near);
		cameraMatrix0.at<double>(14) = cameraMat.at<double>(8);

		cv::Mat M = cv::Mat::eye(4, 4, CV_64F);
		M.at<double>(0) = 2.0 / (double)proWidth;
		M.at<double>(3) = -1;
		M.at<double>(5) = -2.0 / (double)proHeight;
		M.at<double>(7) = 1;

		cameraMatrix = M * cameraMatrix0;
		cameraMatrix.at<double>(5) = -cameraMatrix.at<double>(5);


		// 外部パラメータ
		translate2.at<double>(0) = translate.at<double>(0)/translate.at<double>(3);
		translate2.at<double>(1) = translate.at<double>(1)/translate.at<double>(3);
		translate2.at<double>(2) = translate.at<double>(2)/translate.at<double>(3);

		wornldTranslate = rotation * translate2; //decomposeProjectionMatrix()のせい？

		//decomposeProjectionMatrix()のせいで-色々つけてる？
		cv::Mat externalMat = cv::Mat::eye(4,4,CV_64F);
		externalMat.at<double>(0) = rotation.at<double>(0);
		externalMat.at<double>(1) = rotation.at<double>(1);
		externalMat.at<double>(2) = rotation.at<double>(2);
		externalMat.at<double>(3) = -worldTranslate.at<double>(0);
		externalMat.at<double>(4) = -rotation.at<double>(3);
		externalMat.at<double>(5) = -rotation.at<double>(4);
		externalMat.at<double>(6) = -rotation.at<double>(5);
		externalMat.at<double>(7) = worldTranslate.at<double>(1);
		externalMat.at<double>(8) = rotation.at<double>(6);
		externalMat.at<double>(9) = rotation.at<double>(7);
		externalMat.at<double>(10) = rotation.at<double>(8);
		externalMat.at<double>(11) = -worldTranslate.at<double>(2);


		// 結果の保存
		cv::FileStorage fs2("Calibration/calibration.xml", cv::FileStorage::WRITE);
		cv::write(fs2,"reprojectionError", reprojectionResult);
		cv::write(fs2,"projectorCalibration", perspectiveMat);
		cv::write(fs2,"internalMatrix", cameraMat);
		cv::write(fs2,"cameraMatrix", cameraMatrix);
		cv::write(fs2,"externalMatrix", externalMat);


		for(int i = 0; i < 16; ++i)
		{
			projectionMatrix[i] = cameraMatrix.at<double>(i);
			externalMatrix[i] = externalMat.at<double>(i);
		}
	}


	// Ground truthの保存
    __declspec(dllexport) void saveUnityXML(const char* fileName, double* camera, double* external)
    {
		cv::Mat cameraMat(4, 4, CV_64F, camera);
		cv::Mat externalMat(4, 4, CV_64F, external);

		cv::FileStorage cvfs(fileName, CV_STORAGE_WRITE);
		cv::write(cvfs,"cameraMatrix", cameraMat);
		cv::write(cvfs,"externalMatrix", externalMat);
    }


	// 透視投影変換行列の確認
	__declspec(dllexport) void checkProjection(double srcPointX, double srcPointY, double srcPointZ, double& dstPointX, double& dstPointY, double* perspective)
    {
		cv::Mat perspectiveMat(3, 4, CV_64F, perspective);
		
		cv::Mat srcPoint(4, 1, CV_64F);
		cv::Mat dstPoint(3, 1, CV_64F);

		srcPoint.at<double>(0) = srcPointX;
		srcPoint.at<double>(1) = srcPointY;
		srcPoint.at<double>(2) = srcPointZ;
		srcPoint.at<double>(3) = 1;

		dstPoint = perspectiveMat * srcPoint;

		dstPointX = dstPoint.at<double>(0) / dstPoint.at<double>(2);
		dstPointY = dstPoint.at<double>(1) / dstPoint.at<double>(2);
	}


	// 透視投影変換行列の読み込み
	__declspec(dllexport) void loadPerspectiveMatrix(double projectionMatrix[], double externalMatrix[])
    {
		cv::Mat cameraMat(4, 4, CV_64F);
		cv::Mat externalMat(4, 4, CV_64F);

		cv::FileStorage cvfs("Calibration/calibration.xml", CV_STORAGE_READ);
		cvfs["cameraMatrix"] >> cameraMat;
		cvfs["externalMatrix"] >> externalMat;

		for(int i = 0; i < 16; ++i)
		{
			projectionMatrix[i] = cameraMat.at<double>(i);
			externalMatrix[i] = externalMat.at<double>(i);
		}
	}
}