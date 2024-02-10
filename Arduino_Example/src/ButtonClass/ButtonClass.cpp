/*
Library for a debounced simple button
*/

#include "Arduino.h"
#include "ButtonClass.h"

ButtonClass::ButtonClass(int pin, unsigned long dbncDelay)
{
	_pin = pin;
	_dbncDelay = dbncDelay;

	pinMode(_pin, INPUT_PULLUP);

	_stateOld = digitalRead(_pin);
	_stateNew = _stateOld;
	_stateNewTry = _stateOld;
	_stateOldTry = _stateOld;

	_dbncTime = millis();
}

ButtonStates ButtonClass::ButtonState()
{
	// Read current state of button
	_stateNewTry = digitalRead(_pin);

	// Check if state has changed
	if (_stateNewTry != _stateOldTry)
		// Reset debounce timer
		_dbncTime = millis();

	if ((millis() - _dbncTime) > _dbncDelay)
	{
		if (_stateNewTry != _stateOld)
			// The _stateNewTry is stable for _dbncDelay, and different from _stateOld
			_stateNew = _stateNewTry;
	}

	// Keep the "unbounced" key state
	_stateOldTry = _stateNewTry;

	// Calculate the _returnState
	if (_stateNew == LOW)
	{ // New state = ON
		if (_stateOld == LOW)
			_returnState = ON; // Stable ON
		else
			_returnState = OFF_ON; // From OFF to ON
	}
	else
	{ // New state = OFF
		if (_stateOld == HIGH)
			_returnState = OFF; // Stable OFF
		else
			_returnState = ON_OFF; // From ON to OFF
	}

	// Keep the "debounced" key state
	_stateOld = _stateNew;

	return _returnState;
}

String ButtonClass::ToString()
{
	switch (_returnState)
	{
	case ON:
		return "ON";
	case OFF:
		return "OFF";
	case ON_OFF:
		return "ON_OFF";
	case OFF_ON:
		return "OFF_ON";
	default:
		return "Unknown state";
	}
}