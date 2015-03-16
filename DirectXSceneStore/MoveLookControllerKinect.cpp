#include "pch.h"
#include "MoveLookControllerKinect.h"


MoveLookController^ MoveLookControllerKinect::Create()
{
	auto p = ref new MoveLookControllerKinect();
	return static_cast<MoveLookController^>(p);
}

MoveLookControllerKinect::MoveLookControllerKinect()
{
	
}


