#include "src/ButtonClass/ButtonClass.h"
#include "src/LedControl-1.0.6/LedControl.h"

// Command related items
#define CMD_SIZE 512
char sBuffer[CMD_SIZE];
char sCommand[CMD_SIZE];
char* sParameter;
int iRxPtr = 0;
bool bCmdReady = false;
bool bRegistered = false;

// Global variables for Serial communication
const long baudrate = 500000;

// IDENT
const String sIdent = "TEST Module\n";
const String sProcessor = "ARDUINO\n";

// Properties (Properties start at 001
const char *Properties[] = {
  "INT32_RW_L:A32NX_EFIS_L_OPTION,enum",                // 001
  "VOID_K:A32NX.FCU_HDG_INC",                           // 002
  "VOID_K:A32NX.FCU_HDG_DEC",                           // 003
  "INT32_R_L:A32NX_AUTOPILOT_HEADING_SELECTED,degrees", // 004
  };
size_t nProperties = sizeof(Properties)/sizeof(Properties[0]);

// Easy way to toggle an LED
void ToggleLed(int pin) { digitalWrite(pin, !digitalRead(pin)); }

// Acknowledge
#define ACK Serial.print("A\n")

// Buttons
#define BTN_CSTR_L 40
#define BTN_WPT_L 41
ButtonClass bcBTN_CSTR_L = ButtonClass(BTN_CSTR_L, 50);
ButtonClass bcBTN_WPT_L = ButtonClass(BTN_WPT_L, 50);

// LED
#define LED_CSTR_L 38
#define LED_WPT_L 39
int EFIS_L = 0;

// 7-Segment Display
LedControl lc=LedControl(12,11,10,1);

void displayHDGValue(int iValue)
{
  lc.setDigit(0, 0, iValue % 10, false);
  lc.setDigit(0, 1, (iValue / 10) % 10, false);
  lc.setDigit(0, 2, (iValue / 100) % 10, false);
}

// Encoder
//
// Below Rotary Encoder code originates from https://github.com/mprograms/SimpleRotary
// Released under the GNU Public License v3 (GPLv3)
// I only copied the part that shows if rotary encoder turns CW or CCW to trigger INC and DEC commands

#define ENC_PIN_A 42
#define ENC_PIN_B 43
byte _trigger = HIGH;
unsigned long _currentTime = millis();
unsigned long _debounceRTime = _currentTime;
unsigned long _debounceSTime = _currentTime;
unsigned long _errorTime = _currentTime;
unsigned int _errorDelay = 100;
unsigned int _debounceRDelay = 2;
unsigned int _debounceSDelay = 200;

bool _statusA = false;
bool _statusB = false;
bool _statusA_prev = false;
bool _statusS_prev = false;
byte _errorLast = 0;
    
byte CheckEncoder()
{
  byte _dir = 0x00;
  _currentTime = millis();
  
  if( _currentTime >= ( _debounceRTime + _debounceRDelay ) ) {

    _statusA = ( digitalRead(ENC_PIN_A) == _trigger ? true : false);
    _statusB = ( digitalRead(ENC_PIN_B) == _trigger ? true : false);
   
    if( !_statusA && _statusA_prev ){

      if ( _statusB != _statusA ) {
        _dir = 0x01;
      } else {
        _dir = 0x02;
      }

      if ( _currentTime < (_errorTime + _errorDelay) ){
        _dir = _errorLast;
      } else {
    _errorLast = _dir;
    }

      _errorTime = _currentTime;
    
    }
    _statusA_prev = _statusA;
    _debounceRTime = _currentTime;
  }

  return _dir;
}

void setup() {
  pinMode(LED_BUILTIN, OUTPUT);
  digitalWrite(LED_BUILTIN, LOW);

  // Initialize LED
  pinMode(LED_CSTR_L, OUTPUT);
  digitalWrite(LED_CSTR_L, LOW);
  pinMode(LED_WPT_L, OUTPUT);
  digitalWrite(LED_WPT_L, LOW);

  // Initialize Encoder
  pinMode(ENC_PIN_A, INPUT);
  pinMode(ENC_PIN_B, INPUT);

  // Initialize LCD
  lc.shutdown(0,false);
  lc.setIntensity(0,8);
  lc.clearDisplay(0);

  Serial.begin(baudrate, SERIAL_8N1);
  ACK;
}

void loop() {
  digitalWrite(LED_BUILTIN, LOW);

  if (bCmdReady) {

    // Process IDENT command
    if (strcmp(sCommand, "IDENT") == 0) {
      Serial.print(sIdent);
      Serial.print(sProcessor);
    }

    // Process REGISTER command
    else if (strcmp(sCommand, "REGISTER") == 0) {
      for (int i = 0; i < nProperties; i++)
      {
        Serial.print(Properties[i]);
        Serial.print("\n");
      }
      Serial.print("\n");
      bRegistered = true;
    }

    else if (bRegistered) {
      // Reset bRegistered flag - do not send ACK
      if (strcmp(sCommand, "RESET") == 0)
      {
        bRegistered = false;
      }

      else if ((strlen(sCommand) > 4) && (sCommand[3] == '='))
      {
        // Assumed that "NNN=..." is received
        
        ACK;

        sCommand[3] = '\0';
        sParameter = &sCommand[4];

        int iID = atoi(sCommand);
        if (iID != 0)
        {
          switch (iID) {
            case 1: // INT32_RW_L:A32NX_EFIS_L_OPTION,enum
            {
              // first reset all LEDs
              EFIS_L=0;
              digitalWrite(LED_CSTR_L, LOW);
              digitalWrite(LED_WPT_L, LOW);
              // set the required EFIS_L LED
              int iEFIS = atoi(sParameter);
              switch (iEFIS)
              {
                case 1:
                  digitalWrite(LED_CSTR_L, HIGH);
                  EFIS_L = 1;
                  break;
                case 3:
                  digitalWrite(LED_WPT_L, HIGH);
                  EFIS_L = 3;
                  break;
                default:
                  break;
              }
              break;
            }
            case 4: // INT32_R_L:A32NX_AUTOPILOT_HEADING_SELECTED,Degrees
            {
              int iHDG = atoi(sParameter);
              displayHDGValue(iHDG);
              break;
            }
            default:
              break;
          }
        }
      }
    }

    bCmdReady = false;
  }

  if (bRegistered) {
    // Process CSTR_L Button
    if (bcBTN_CSTR_L.ButtonState() == OFF_ON) 
    {
      EFIS_L = (EFIS_L == 1) ? 0 : 1;
      char buffer[10];
      sprintf(buffer, "001=%d\n", EFIS_L);
      Serial.print(buffer);
    }
    // Process WPT_L Button
    if (bcBTN_WPT_L.ButtonState() == OFF_ON)
    {
      EFIS_L = (EFIS_L == 3) ? 0 : 3;
      char buffer[10];
      sprintf(buffer, "001=%d\n", EFIS_L);
      Serial.print(buffer);
    }

    byte bEnc = CheckEncoder();
    if (bEnc == 1) // CW
      Serial.print("002=\n");
    else if (bEnc == 2) // CCW
      Serial.print("003=\n");
  }
}

void serialEvent()
{
  while (Serial.available())
  {
    char cCmd = Serial.read();
    if (cCmd == '\n')
    {
      sBuffer[iRxPtr] = 0;        // terminate string
      strcpy(sCommand, sBuffer);  // copy sBuffer in sCommand
      bCmdReady = true;           // indicate that command is available
      iRxPtr = 0;
    }
    else if (cCmd != '\r')
      sBuffer[iRxPtr++] = cCmd;
  }
}
