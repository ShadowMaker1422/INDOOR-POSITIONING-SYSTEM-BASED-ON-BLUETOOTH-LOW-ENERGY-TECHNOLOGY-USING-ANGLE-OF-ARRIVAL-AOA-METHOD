#include <WiFi.h>
#include <PubSubClient.h>
#include <algorithm>

#define RX_PIN 16
#define TX_PIN 17
#define BAUD_RATE 115200
String dataBuffer = "";
HardwareSerial MySerial(1);

int a = 0;
int e = 0;

const char* WIFI_SSID = "TP-LINK_0996";
const char* WIFI_PASSWORD = "36340200";

const char* MQTT_SERVER = "192.168.0.102";
const int MQTT_PORT = 1883;
const char* MQTT_TOPIC_PUB = "cuong151";
const char* MQTT_TOPIC_SUB = "receive";

#define WINDOW_SIZE 5
int aBuffer[WINDOW_SIZE];
int eBuffer[WINDOW_SIZE];
int aBufferIndex = 0;
int eBufferIndex = 0;

WiFiClient espClient;
PubSubClient mqttClient(espClient);

unsigned long previousWiFiCheckTime = 0;

void connectWiFi() {
  Serial.print("Connecting to WiFi...");
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  while (WiFi.status() != WL_CONNECTED) {
    Serial.print(".");
    delay(100);
    if (millis() - previousWiFiCheckTime > 10000) {
      Serial.println("\nWiFi failed, retrying...");
      return;
    }
  }
  Serial.println("\nWiFi connected. IP: " + WiFi.localIP().toString());
}

void callback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Message arrived [");
  Serial.print(topic);
  Serial.print("]: ");
  for (int i = 0; i < length; i++) {
    Serial.print((char)payload[i]);
  }
  Serial.println();
}

void reconnectMQTT() {
  while (!mqttClient.connected()) {
    Serial.print("Attempting MQTT connection...");
    String clientId = "ESP32Client-" + String(random(0xffff), HEX);
    if (mqttClient.connect(clientId.c_str())) {
      Serial.println("Connected to MQTT");
      mqttClient.subscribe(MQTT_TOPIC_SUB);
    } else {
      Serial.print("Failed, rc=");
      Serial.print(mqttClient.state());
      Serial.println(" Retry in 2 seconds...");
      delay(2000);
    }
  }
}

void addToBuffer(int* buffer, int value, int& bufferIndex) {
  buffer[bufferIndex] = value;
  bufferIndex = (bufferIndex + 1) % WINDOW_SIZE;
}

int getMedian(int* buffer) {
  int sorted[WINDOW_SIZE];
  memcpy(sorted, buffer, WINDOW_SIZE * sizeof(int));
  std::sort(sorted, sorted + WINDOW_SIZE);
  return sorted[WINDOW_SIZE / 2];
}

void processData(String& dataBuffer) {
  int startIndex = dataBuffer.indexOf(":") + 1;
  int firstCommaIndex = dataBuffer.indexOf(",", startIndex);
  String deviceID = dataBuffer.substring(startIndex, firstCommaIndex);

String values = dataBuffer.substring(startIndex);

  int commaIndex1 = values.indexOf(',');
  int commaIndex2 = values.indexOf(',', commaIndex1 + 1);
  int commaIndex3 = values.indexOf(',', commaIndex2 + 1);
  int commaIndex4 = values.indexOf(',', commaIndex3 + 1);

  a = values.substring(commaIndex2 + 1, commaIndex3).toInt();
  e = values.substring(commaIndex3 + 1, commaIndex4).toInt();

  addToBuffer(aBuffer, a, aBufferIndex);
  addToBuffer(eBuffer, e, eBufferIndex);

  int medianA = getMedian(aBuffer);
  int medianE = getMedian(eBuffer);

  String filteredData = "AC2," + deviceID + "," + String(medianA) + "," + String(medianE);
  //String filteredData = "AC2," + deviceID + "," + String(a) + "," + String(e);
  mqttClient.publish(MQTT_TOPIC_PUB, filteredData.c_str());
  Serial.println("Sent valid data via MQTT: " + filteredData);
}

bool isValidData(const String& data) {
  if (!data.startsWith("+UUDF:")) return false;

  int commaCount = 0;
  for (char c : data) {
    if (c == ',') commaCount++;
  }
  return commaCount >= 9;
}

void setup() {
  Serial.begin(115200);
  connectWiFi();
  mqttClient.setServer(MQTT_SERVER, MQTT_PORT);
  mqttClient.setCallback(callback);
  mqttClient.setKeepAlive(15);

  MySerial.setRxBufferSize(2048);
  MySerial.begin(115200, SERIAL_8N1, RX_PIN, TX_PIN);
  Serial.println("UART1 initialized.");
}

void loop() {
  if (WiFi.status() != WL_CONNECTED) {
    connectWiFi();
  }

  if (!mqttClient.connected()) {
    reconnectMQTT();
  }
  mqttClient.loop();
  while (MySerial.available()) {
    char c = MySerial.read();
    dataBuffer += c;

    if (c == '\n') {
      if (isValidData(dataBuffer)) {
        processData(dataBuffer);
      }
      dataBuffer = ""; 
    }
  }
}
