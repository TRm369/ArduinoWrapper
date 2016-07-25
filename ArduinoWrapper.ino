//Constants declaration
const byte Beginning[] = { 0x48, 0x69 };
byte ACK[] = { 0x06 };
byte NAK[] = { 0x15 };
const int Pow2_8 = 256;
const int Pow2_16 = 65536;
const int Pow2_24 = 16777216;
const byte pinModeCommand = 0x20;
const byte digitalWriteCommand = 0x21;
const byte digitalReadCommand = 0x30;
const byte ConnectCommand = 0x11;
const byte DisconnectCommand = 0x13;
const byte KeepAliveCommand = 0x16;
const int maxMessageLength = 64;
const int PinCount = 54;
const int ParameterCount = 3;

//Variables declaration
bool ReadPins[PinCount];
bool ReadPinsStates[PinCount];
byte* Data;
int DataLength;
int Checksum;
int Sum;
byte command[3]; //used for temporary holding of commands before adding them to Data
int i; //used in for loops/as temp value holder
byte* DataToSend; //used in Write
int DataLengthToSend;
int Parameters[ParameterCount];
int timeLeft;
bool Connected = false;
int ResendCount = 0;
unsigned long oldMillis;
int KeepAliveTimeLeft;

void setup() {
  pinMode(2, OUTPUT); //Connected LED
  for (i = 0; i < PinCount; i++) {
    ReadPins[i] = false;
    ReadPinsStates[i] = false;
  }
  DataToSend = (byte*) malloc(maxMessageLength - 5); //Prepare DataToSend for writing
  Serial.begin(9600);
}

void loop() {
  digitalWrite(2, Connected); //Connected LED
  if (Connected)
    loopConnected();
  else
    loopNotConnected();
}

void loopNotConnected() {
  if (Read()){ //Read data if any,          
    if (CheckData(Data, DataLength, Checksum)) { //check if message is in tact using checksum
      Write(ACK,1);
      
      if (Data[0] == ConnectCommand) { //If received message is PC trying to connect
        for (i = 0; i < (DataLength - 2) / 4; i++) { 
          Parameters[i + Data[1]] = ByteArrayToInt(Data, 2 + 4*i); //grag all the parameters it's sending
        }
        if ((DataLength - 2) / 4 + Data[1] == ParameterCount) { //This was the last of parameters, we are now sucecssfully connected
          DataToSend[0] = 0x12; //Tell the PC we are now connected
          DataLengthToSend = 1;
          WriteData();
          Connected = true; //And set mode to connected
          KeepAliveTimeLeft = Parameters[2];
          DeltaTime(); //Call DeltaTime so the next time it's called it returns delta time from now
        }
      }
    }
  }
}

void loopConnected() {
  //KeepAlive check
  KeepAliveTimeLeft = KeepAliveTimeLeft - DeltaTime();
  if (KeepAliveTimeLeft < 0) {    
    Connected = false;
    return;
  }
  
  //WRITE
  PrepareWriteData();      //Prepare any data that need to be sent
  WriteData();             //and send them
  
  //READ
  if (Read()){ //Read data if any,          
    if (CheckData(Data, DataLength, Checksum)) { //check if message is in tact using checksum
      Write(ACK,1);
      ProcessCommands(Data, DataLength);
    }//If data are corrupted do nothing PC will send them again after ACKtimeout
  }
}

bool CheckData (byte* data, int dataLength, byte checksum) {
  Sum = 0;
  for (i = 0; i < dataLength; i++)
    Sum = Sum + data[i];
  return Sum % 256 == checksum;
}

void PrepareWriteData() {
  bool pinState;
  command[0] = digitalReadCommand;
  for (i = 0; i < PinCount; i++) { //Go through all pins
    if (ReadPins[i]) { //Check if they're flagged to be read
      pinState = digitalRead(i);
      if (pinState != ReadPinsStates[i]){ //If value changed tell the host
        ReadPinsStates[i] = pinState;
        command[1] = i;
        command[2] = (byte)pinState;
        AddDataToMessage(command, 3);
      }
    }
  }
}

void ProcessCommands(byte* data, int dataLength) {
  int index = 0;
  while (dataLength - index > 2) {
    //Execute command:
    //setPinMode command
    if (data[index] == pinModeCommand){
      if (data[index + 2] == 0) {
        pinMode(data[index + 1], INPUT);
        ReadPins[data[index + 1]] = true;
      }
      if (data[index + 2] == 1) {
        pinMode(data[index + 1], OUTPUT);
        ReadPins[data[index + 1]] = false;
      }
      if (data[index + 2] == 2) {
        pinMode(data[index + 1], INPUT_PULLUP);
        ReadPins[data[index + 1]] = true;
      }
    }
    //digitalWrite command
    if (data[index] == digitalWriteCommand){
      digitalWrite(data[index + 1], data[index + 2]);
    }
    //KeepAlive command
    if (data[index] == KeepAliveCommand){
      KeepAliveTimeLeft = Parameters[2];
    }
    //Disconnect command
    if (data[index] == DisconnectCommand){
      Connected = false;
      return;
    }
    //Command processed, move on to next command
    index = index + 3;
  }
}

bool Read() {
  //Is message ready?
  if (Serial.available() > 1) { //Two or more bytes ready, check if they are a beggining of a message
    if (Serial.read() == Beginning[0]) {
      if (Serial.read() == Beginning[1]) {
        //Beggining received, read data length and checksum
        while (Serial.available() < 3) { }
        free(Data);
        Data = new byte[2];
        Serial.readBytes(Data, 2);
        DataLength = ByteArrayToShort(Data);
        Checksum = Serial.read();
        //Read data
        while (Serial.available() < DataLength) { }
        free(Data);
        Data = new byte[DataLength];
        Serial.readBytes(Data, DataLength);
        return true;
      }
    }
  }
  return false;
}

void AddDataToMessage (byte* data, int dataLength) {
  if (DataLengthToSend + dataLength > maxMessageLength - 5) { //Check if there's room for the data in current message
    WriteData(); //If not send this message
  }
  
  for (i = 0; i < dataLength; i++)
    DataToSend[DataLengthToSend + i] = data[i];
  DataLengthToSend = DataLengthToSend + dataLength;
}

void WriteData() {
  if (DataLengthToSend == 0)
    return;

  Write(DataToSend, DataLengthToSend);
  timeLeft = Parameters[0];
  while (timeLeft > 0) {
    if (Read()) {
      if (CheckData(Data, DataLength, Checksum)) { //check if message is in tact using checksum
        if (Data[0] = ACK[0]){ //Clean up and return
          DataLengthToSend = 0;
          ResendCount = 0;
          return;
        }
        else {
          Write(ACK,1);
          ProcessCommands(Data, DataLength);
        }
      }
    }
    timeLeft = timeLeft - 10;
    delay(10);
  }
  if (ResendCount < Parameters[1]) {
    ResendCount++;
    WriteData();
  } else { //Clean up and return
    DataLengthToSend = 0;
    ResendCount = 0;
  }
}

void Write(byte* data, int dataLength) {
  if (dataLength == 0)
    return;

  Sum = 0;
  for (i = 0; i < dataLength; i++)
    Sum = Sum + data[i];
  Checksum = Sum % 256;
  
  Serial.write(Beginning, 2);
  Serial.write(ShortToByteArray(dataLength),2);
  Serial.write(Checksum);
  Serial.write(data, dataLength);
}

int DeltaTime() {
  i = millis() - oldMillis;
  oldMillis = millis();
  if
  return i;
}

short ByteArrayToShort (byte value[]) {
  return (short)(value[0] * Pow2_8 + value[1]);
}

byte* ShortToByteArray (short value) {
  byte* Array = new byte[2];
  short temp = value;
  Array[0] = temp / Pow2_8;
  Array[1] = temp % Pow2_8;

  return Array;
}

int ByteArrayToInt (byte* value, int offset) {
  return value[offset] * Pow2_24 + value[1 + offset] * Pow2_16 + value[2 + offset] * Pow2_8 + value[3 + offset];
}

