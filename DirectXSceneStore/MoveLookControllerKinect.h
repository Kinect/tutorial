#pragma once
#include "GameContent\MoveLookController.h"
ref class MoveLookControllerKinect : public MoveLookController
{
public:

internal:
	static MoveLookController^ Create();

private:
	MoveLookControllerKinect();
	
};

