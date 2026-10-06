/*
 * PM5 BWM Capability Probe — ESP32-C2 / ESP8684
 *
 * Protocol MUST match src/PM5Control.Core/WirelessLab/WirelessProtocol.cs:
 *   SOF 0xAA | CMD | LEN | PAYLOAD | CRC-8/CCITT | EOF 0x55
 *   CRC polynomial 0x07, initial value 0x00.
 *
 * This firmware validates benign Wi-Fi capabilities. It deliberately does
 * not implement deauthentication/disassociation injection.
 */
#include <stdbool.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include "driver/uart.h"
#include "esp_err.h"
#include "esp_event.h"
#include "esp_netif.h"
#include "esp_wifi.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "nvs_flash.h"

#define UART_NUM UART_NUM_0
#define UART_BUF_SIZE 512
#define SOF 0xAA
#define EOF_MARK 0x55

#define CAP_SOFTAP 0x01
#define CAP_STA 0x02
#define CAP_SCAN 0x03
#define CAP_PROMISCUOUS 0x04
#define CAP_BEACON_TX 0x05
#define CAP_PROBE_REQ_TX 0x06
#define CAP_PROBE_RSP_TX 0x07
#define CAP_ACTION_TX 0x08
#define CAP_DEAUTH_TX 0x09
#define CAP_DISASSOC_TX 0x0A
#define CAP_APSTA 0x0B

#define RESULT_PASS 0x01
#define RESULT_FAIL 0x02
#define RESULT_ERROR 0x03

#define CMD_PING 0x01
#define CMD_GET_CAPABILITIES 0x02
#define CMD_RUN_TEST 0x03
#define CMD_START_SOFTAP 0x04
#define CMD_STOP_SOFTAP 0x05
#define CMD_START_SCAN 0x06
#define CMD_START_SNIFFER 0x07
#define CMD_STOP_SNIFFER 0x08
#define CMD_SET_POWER_MODE 0x09
#define CMD_GET_STATUS 0x0A

#define EVT_PONG 0x81
#define EVT_CAP_RESULT 0x82
#define EVT_SCAN_RESULT 0x84
#define EVT_AP_CLIENT 0x86
#define EVT_STATUS 0x87
#define EVT_ERROR 0xFF

static volatile bool softap_running;
static volatile bool sniffer_running;

static uint8_t crc8_ccitt(const uint8_t *data, size_t len)
{
    uint8_t crc = 0;
    while (len--) {
        crc ^= *data++;
        for (int i = 0; i < 8; ++i)
            crc = (uint8_t)((crc & 0x80) ? ((crc << 1) ^ 0x07) : (crc << 1));
    }
    return crc;
}

static void send_frame(uint8_t cmd, const uint8_t *payload, uint8_t len)
{
    uint8_t frame[UART_BUF_SIZE];
    size_t total = (size_t)len + 5U;
    if (total > sizeof(frame)) return;
    frame[0] = SOF;
    frame[1] = cmd;
    frame[2] = len;
    if (len && payload) memcpy(&frame[3], payload, len);
    frame[3 + len] = crc8_ccitt(&frame[1], (size_t)len + 2U);
    frame[4 + len] = EOF_MARK;
    uart_write_bytes(UART_NUM, (const char *)frame, total);
}

static void send_result(uint8_t id, uint8_t result, esp_err_t err)
{
    uint8_t p[3] = { id, result, result == RESULT_PASS ? 0 : (uint8_t)err };
    send_frame(EVT_CAP_RESULT, p, sizeof(p));
}

static esp_err_t start_mode(wifi_mode_t mode)
{
    esp_err_t err = esp_wifi_set_mode(mode);
    if (err == ESP_OK) err = esp_wifi_start();
    return err;
}

static void stop_wifi(void)
{
    (void)esp_wifi_set_promiscuous(false);
    (void)esp_wifi_stop();
    softap_running = false;
    sniffer_running = false;
}

static void test_softap(void)
{
    wifi_config_t cfg = {0};
    memcpy(cfg.ap.ssid, "PM5_CAP_TEST", 12);
    cfg.ap.ssid_len = 12;
    cfg.ap.channel = 1;
    cfg.ap.max_connection = 1;
    cfg.ap.authmode = WIFI_AUTH_OPEN;

    esp_err_t err = esp_wifi_set_mode(WIFI_MODE_AP);
    if (err == ESP_OK) err = esp_wifi_set_config(WIFI_IF_AP, &cfg);
    if (err == ESP_OK) err = esp_wifi_start();
    if (err == ESP_OK) stop_wifi();
    send_result(CAP_SOFTAP, err == ESP_OK ? RESULT_PASS : RESULT_ERROR, err);
}

static void test_sta(void)
{
    esp_err_t err = start_mode(WIFI_MODE_STA);
    if (err == ESP_OK) stop_wifi();
    send_result(CAP_STA, err == ESP_OK ? RESULT_PASS : RESULT_ERROR, err);
}

static void test_scan(void)
{
    esp_err_t err = start_mode(WIFI_MODE_STA);
    if (err == ESP_OK) {
        wifi_scan_config_t cfg = {
            .ssid = NULL, .bssid = NULL, .channel = 0,
            .show_hidden = true, .scan_type = WIFI_SCAN_TYPE_ACTIVE,
            .scan_time.active.min = 100, .scan_time.active.max = 300
        };
        err = esp_wifi_scan_start(&cfg, true);
    }
    if (err == ESP_OK) {
        uint16_t count = 0;
        err = esp_wifi_scan_get_ap_num(&count);
    }
    stop_wifi();
    send_result(CAP_SCAN, err == ESP_OK ? RESULT_PASS : RESULT_FAIL, err);
}

static void test_promiscuous(void)
{
    esp_err_t err = start_mode(WIFI_MODE_STA);
    if (err == ESP_OK) err = esp_wifi_set_promiscuous(true);
    if (err == ESP_OK) err = esp_wifi_set_promiscuous(false);
    stop_wifi();
    send_result(CAP_PROMISCUOUS, err == ESP_OK ? RESULT_PASS : RESULT_FAIL, err);
}

static void test_apsta(void)
{
    esp_err_t err = start_mode(WIFI_MODE_APSTA);
    stop_wifi();
    send_result(CAP_APSTA, err == ESP_OK ? RESULT_PASS : RESULT_ERROR, err);
}

/* Benign API-level beacon probe. It does not claim external RF observation. */
static void test_beacon_tx(void)
{
    static const uint8_t frame[] = {
        0x80,0x00,0x00,0x00,
        0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
        0xDE,0xAD,0xBE,0xEF,0x00,0x01,
        0xDE,0xAD,0xBE,0xEF,0x00,0x01,
        0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
        0x64,0x00,0x01,0x04
    };
    esp_err_t err = start_mode(WIFI_MODE_STA);
    if (err == ESP_OK)
        err = esp_wifi_80211_tx(WIFI_IF_STA, frame, sizeof(frame), true);
    stop_wifi();
    send_result(CAP_BEACON_TX, err == ESP_OK ? RESULT_PASS : RESULT_FAIL, err);
}

static void sniffer_cb(void *buf, wifi_promiscuous_pkt_type_t type)
{
    (void)buf; (void)type;
    /* Driver callback only: no UART I/O here. */
}

static void wifi_event_handler(void *arg, esp_event_base_t base, int32_t id, void *data)
{
    (void)arg; (void)base;
    if (id == WIFI_EVENT_AP_STACONNECTED || id == WIFI_EVENT_AP_STADISCONNECTED) {
        uint8_t p[7] = { id == WIFI_EVENT_AP_STACONNECTED ? 1 : 0 };
        if (id == WIFI_EVENT_AP_STACONNECTED)
            memcpy(&p[1], ((wifi_event_ap_staconnected_t *)data)->mac, 6);
        else
            memcpy(&p[1], ((wifi_event_ap_stadisconnected_t *)data)->mac, 6);
        send_frame(EVT_AP_CLIENT, p, sizeof(p));
    } else if (id == WIFI_EVENT_SCAN_DONE) {
        uint16_t count = 0;
        if (esp_wifi_scan_get_ap_num(&count) != ESP_OK || count == 0) return;
        wifi_ap_record_t *list = calloc(count, sizeof(*list));
        if (!list) return;
        uint16_t n = count;
        if (esp_wifi_scan_get_ap_records(&n, list) == ESP_OK) {
            for (uint16_t i = 0; i < n; ++i) {
                uint8_t p[64]; uint8_t k = 0;
                p[k++] = list[i].ssid[0] ? 1 : 0;
                memcpy(&p[k], list[i].ssid, 32); k += 32;
                memcpy(&p[k], list[i].bssid, 6); k += 6;
                p[k++] = list[i].primary;
                p[k++] = (uint8_t)list[i].rssi;
                p[k++] = (uint8_t)list[i].authmode;
                p[k++] = list[i].pmf_cfg.required ? 2 : (list[i].pmf_cfg.capable ? 1 : 0);
                send_frame(EVT_SCAN_RESULT, p, k);
            }
        }
        free(list);
    }
}

static void process_cmd(uint8_t cmd, const uint8_t *payload, uint8_t len)
{
    switch (cmd) {
    case CMD_PING: send_frame(EVT_PONG, payload, len); break;
    case CMD_GET_CAPABILITIES:
        test_softap(); test_sta(); test_scan(); test_promiscuous(); test_apsta(); test_beacon_tx();
        break;
    case CMD_RUN_TEST:
        if (len < 1) { uint8_t e=1; send_frame(EVT_ERROR,&e,1); break; }
        switch (payload[0]) {
        case CAP_SOFTAP: test_softap(); break;
        case CAP_STA: test_sta(); break;
        case CAP_SCAN: test_scan(); break;
        case CAP_PROMISCUOUS: test_promiscuous(); break;
        case CAP_BEACON_TX: test_beacon_tx(); break;
        case CAP_APSTA: test_apsta(); break;
        case CAP_PROBE_REQ_TX: case CAP_PROBE_RSP_TX: case CAP_ACTION_TX:
        case CAP_DEAUTH_TX: case CAP_DISASSOC_TX: {
            uint8_t p[3]={payload[0],RESULT_ERROR,0x7F}; send_frame(EVT_CAP_RESULT,p,3); break;
        }
        default: { uint8_t e=2; send_frame(EVT_ERROR,&e,1); break; }
        }
        break;
    case CMD_START_SOFTAP:
        if (len < 33) { uint8_t e=1; send_frame(EVT_ERROR,&e,1); break; }
        {
            wifi_config_t cfg={0};
            size_t n=strnlen((const char*)payload,32);
            memcpy(cfg.ap.ssid,payload,n); cfg.ap.ssid_len=n;
            cfg.ap.channel=payload[32]; cfg.ap.max_connection=4; cfg.ap.authmode=WIFI_AUTH_OPEN;
            esp_err_t err=esp_wifi_set_mode(WIFI_MODE_AP);
            if (err==ESP_OK) err=esp_wifi_set_config(WIFI_IF_AP,&cfg);
            if (err==ESP_OK) err=esp_wifi_start();
            if (err==ESP_OK) { softap_running=true; send_frame(EVT_STATUS,payload,33); }
            else { uint8_t p[2]={CMD_START_SOFTAP,(uint8_t)err}; send_frame(EVT_ERROR,p,2); }
        }
        break;
    case CMD_STOP_SOFTAP: stop_wifi(); break;
    case CMD_START_SCAN: {
        esp_err_t err=start_mode(WIFI_MODE_STA);
        if (err==ESP_OK) { wifi_scan_config_t cfg={.ssid=NULL,.bssid=NULL,.channel=0,.show_hidden=true,.scan_type=WIFI_SCAN_TYPE_ACTIVE,.scan_time.active.min=100,.scan_time.active.max=300}; err=esp_wifi_scan_start(&cfg,false); }
        if (err!=ESP_OK) { uint8_t p[2]={CMD_START_SCAN,(uint8_t)err}; send_frame(EVT_ERROR,p,2); }
        break;
    }
    case CMD_START_SNIFFER:
        if (len<1 || payload[0]<1 || payload[0]>14) { uint8_t e=5; send_frame(EVT_ERROR,&e,1); break; }
        {
            esp_err_t err=start_mode(WIFI_MODE_STA);
            if (err==ESP_OK) err=esp_wifi_set_promiscuous_rx_cb(sniffer_cb);
            if (err==ESP_OK) err=esp_wifi_set_channel(payload[0],WIFI_SECOND_CHAN_NONE);
            if (err==ESP_OK) err=esp_wifi_set_promiscuous(true);
            if (err==ESP_OK) sniffer_running=true;
            else { uint8_t p[2]={CMD_START_SNIFFER,(uint8_t)err}; send_frame(EVT_ERROR,p,2); }
        }
        break;
    case CMD_STOP_SNIFFER: sniffer_running=false; (void)esp_wifi_set_promiscuous(false); break;
    case CMD_GET_STATUS: { uint8_t p[4]={softap_running?1:0,sniffer_running?1:0,0,0}; send_frame(EVT_STATUS,p,4); break; }
    case CMD_SET_POWER_MODE: { uint8_t p[2]={CMD_SET_POWER_MODE,0x7F}; send_frame(EVT_ERROR,p,2); break; }
    default: { uint8_t e=3; send_frame(EVT_ERROR,&e,1); break; }
    }
}

static void uart_task(void *arg)
{
    (void)arg; uint8_t rx[UART_BUF_SIZE]; size_t used=0;
    while (true) {
        uint8_t ch; if (uart_read_bytes(UART_NUM,&ch,1,pdMS_TO_TICKS(10))<=0) continue;
        if (used==0 && ch!=SOF) continue;
        if (used>=sizeof(rx)) { used=0; continue; }
        rx[used++]=ch;
        if (used>=3) {
            uint8_t len=rx[2]; size_t total=(size_t)len+5U;
            if (total>sizeof(rx)) { used=0; continue; }
            if (used==total) {
                size_t crc_index=3U+len;
                if (rx[total-1]==EOF_MARK && crc8_ccitt(&rx[1],(size_t)len+2U)==rx[crc_index])
                    process_cmd(rx[1],&rx[3],len);
                used=0;
            }
        }
    }
}

void app_main(void)
{
    esp_err_t err=nvs_flash_init();
    if (err==ESP_ERR_NVS_NO_FREE_PAGES || err==ESP_ERR_NVS_NEW_VERSION_FOUND) { ESP_ERROR_CHECK(nvs_flash_erase()); err=nvs_flash_init(); }
    ESP_ERROR_CHECK(err);
    const uart_config_t cfg={.baud_rate=115200,.data_bits=UART_DATA_8_BITS,.parity=UART_PARITY_DISABLE,.stop_bits=UART_STOP_BITS_1,.flow_ctrl=UART_HW_FLOWCTRL_DISABLE};
    ESP_ERROR_CHECK(uart_param_config(UART_NUM,&cfg));
    ESP_ERROR_CHECK(uart_driver_install(UART_NUM,UART_BUF_SIZE*2,UART_BUF_SIZE*2,0,NULL,0));
    ESP_ERROR_CHECK(esp_netif_init());
    ESP_ERROR_CHECK(esp_event_loop_create_default());
    esp_netif_create_default_wifi_ap(); esp_netif_create_default_wifi_sta();
    wifi_init_config_t wifi= WIFI_INIT_CONFIG_DEFAULT();
    ESP_ERROR_CHECK(esp_wifi_init(&wifi));
    ESP_ERROR_CHECK(esp_event_handler_instance_register(WIFI_EVENT,ESP_EVENT_ANY_ID,&wifi_event_handler,NULL,NULL));
    xTaskCreate(uart_task,"uart_task",4096,NULL,5,NULL);
    const uint8_t hello[4]={1,1,0,0}; send_frame(EVT_STATUS,hello,sizeof(hello));
}
