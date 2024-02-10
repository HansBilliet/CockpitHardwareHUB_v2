/*
Library for a debounced simple button
*/

#ifndef ButtonClass_h
#define ButtonClass_h

#include "Arduino.h"

enum ButtonStates
{
	ON,
	OFF,
	ON_OFF,
	OFF_ON
};

class ButtonClass
{
public:
	ButtonClass(int pin, unsigned long dbncDelay);
	ButtonStates ButtonState();
	String ToString();
private:
	int _pin;
	unsigned long _dbncDelay;
	int _stateNewTry;
	int _stateOldTry;
	int _stateNew;
	int _stateOld;
	ButtonStates _returnState;
	unsigned long _dbncTime;
};

#endif
