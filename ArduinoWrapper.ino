//Constants declaration
const byte Beginning[] = { 0x48, 0x69 };
byte ACK[] = { 0x06 };
byte NAK[] = { 0x15 };
const int Pow2_8 = 2 ^ 8;
const int Pow2_16 = 2 ^ 16;
const int Pow2_24 = 2 ^ 24;
const int pinModeCommand = 0x20;
const int digitalWriteCommand = 0x21;
const int digitalReadCommand = 0x30;
const int maxMessageLength = 64;
const int PinCount = 54;

//Variables declaration
bool ReadPins[PinCount];
bool ReadPinsStates[PinCount];
byte* Data;
int DataLength;
int Checksum;
int Sum;
byte command[3]; //used for temporary holding of commands before adding them to Data
int i; //used in for loops
byte* DataToSend; //used in Write
int DataLengthToSend;
int ACKtimeout = 1000;
int timeLeft;

void setup() {
  for (i = 0; i < PinCount; i++) {
    ReadPins[i] = false;
    ReadPinsStates[i] = false;
  }
  DataToSend = (byte*) malloc(maxMessageLength - 5); //Prepare DataToSend for writing
  Serial.begin(9600);
}

void loop() {
  
  //READ
  if (Read()){ //Read data if any,          
    if (CheckData(Data, DataLength, Checksum)) { //check if message is in tact using checksum
      Write(ACK,1);
      ProcessCommands(Data, DataLength);
    } else //If data are corrupted do nothing PC will send them again
      Write(NAK,1);
  }
  
  //WRITE
  PrepareWriteData();      //Prepare any data that need to be sent
  WriteData();                 //and send them
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
  timeLeft = ACKtimeout;
  while (timeLeft > 0) {
    if (Read()) {
      if (CheckData(Data, DataLength, Checksum)) { //check if message is in tact using checksum
        if (Data[0] = ACK[0]){ //Clean up and return
          DataLengthToSend = 0;
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
  WriteData();
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
