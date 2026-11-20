#include <WiFi.h>
#include <HTTPClient.h>

// 1. Wi-Fi 配置
const char* ssid = "dbgang";
const char* password = "jhjn20130119";

// 2. 上位机服务地址（请将 192.168.1.X 替换为你电脑的实际局域网 IP）
const char* serverUrl = "http://192.168.1.154:9991/trigger-screenshot"; 

// 3. 按键引脚定义 (ESP32-C3 的 GPIO2)
const int buttonPin = 2;

// 防抖与状态变量
unsigned long lastDebounceTime = 0;
const unsigned long debounceDelay = 50; // 50ms 去抖动延迟
int lastButtonState = HIGH;             // 上一次循环读取的原始电平
int buttonState = HIGH;                 // 经过防抖确认的稳定按键状态

void setup() {
  Serial.begin(115200);
  delay(1000);

  // 初始化按键引脚，启用内部上拉电阻（未按下时为 HIGH，按下时为 LOW）
  pinMode(buttonPin, INPUT_PULLUP);

  // 连接 Wi-Fi
  WiFi.begin(ssid, password);
  Serial.print("正在连接 Wi-Fi");
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("\nWi-Fi 连接成功！");
  Serial.println("系统就绪，按下 GPIO2 按钮即可触发上位机截图。");
}

void loop() {
  // 确保 Wi-Fi 保持连接
  if (WiFi.status() == WL_CONNECTED) {
    int reading = digitalRead(buttonPin);

    // 如果检测到电平发生跳变（可能由按键抖动引起），重置防抖计时器
    if (reading != lastButtonState) {
      lastDebounceTime = millis();
    }

    // 检查电平状态是否维持超过了 50ms（过滤毛刺）
    if ((millis() - lastDebounceTime) > debounceDelay) {
      // 只有当稳定后的状态与之前记录的稳定状态不同时才处理
      if (reading != buttonState) {
        buttonState = reading;

        // 如果稳定后的状态是 LOW，说明按键被确实按下了
        if (buttonState == LOW) {
          Serial.println("\n[事件] 检测到按键有效按下，正在发送截图请求...");
          triggerScreenshot();
        }
      }
    }

    // 更新上一次的原始电平状态
    lastButtonState = reading;
  } else {
    Serial.println("Wi-Fi 断开，正在尝试重连...");
    WiFi.reconnect();
    delay(5000);
  }
}

void triggerScreenshot() {
  HTTPClient http;

  Serial.println("正在向服务器发送 GET 请求...");
  http.begin(serverUrl);
  
  // 设置超时时间（例如 5 秒）
  http.setTimeout(5000);

  int httpResponseCode = http.GET(); 

  if (httpResponseCode > 0) {
    String response = http.getString();
    Serial.printf("请求成功！HTTP 状态码: %d\n", httpResponseCode);
    Serial.println("响应内容: " + response);
  } else {
    Serial.printf("请求失败，错误码: %d (请检查电脑 IP、防火墙及 9991 端口)\n", httpResponseCode);
  }

  http.end();
}