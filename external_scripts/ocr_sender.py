import time
import requests
import random

STOCK_EASE_SERVER_URL = "YOUR_LINK/api/weightintegration/screendata"
SEND_INTERVAL_SECONDS = 2
SENSOR_ID = "SENSOR_01"
API_KEY = "jay_nepal_this_is_a_very_secret_key"
MOCK_START_VALUE = 100.0
MOCK_INCREMENT = 1.0
MOCK_RANDOM_RANGE = 5.0
MOCK_RESET_THRESHOLD = 1000.0


def main():
    print("--- RUNNING IN MOCK DATA MODE (Internal Generator) ---")
    print(f"Target URL: {STOCK_EASE_SERVER_URL}")
    print(f"Send Interval: {SEND_INTERVAL_SECONDS} seconds")
    print(f"Sensor ID: {SENSOR_ID}")

    mock_counter = MOCK_START_VALUE
    last_sent_number = None

    while True:
        try:
            base_value = mock_counter
            random_offset = random.uniform(-MOCK_RANDOM_RANGE, MOCK_RANDOM_RANGE)
            current_number = round(base_value + random_offset, 2)

            print(f"Generated Mock Value: {current_number} (Base: {base_value:.2f})")

            mock_counter += MOCK_INCREMENT

            if mock_counter > MOCK_RESET_THRESHOLD:
                print(
                    f"Mock counter reached {MOCK_RESET_THRESHOLD}, resetting to {MOCK_START_VALUE}"
                )
                mock_counter = MOCK_START_VALUE

            if current_number != last_sent_number:
                try:
                    headers = {"X-API-Key": API_KEY, "Content-Type": "application/json"}
                    payload = {"sensorId": SENSOR_ID, "value": current_number}
                    response = requests.post(
                        STOCK_EASE_SERVER_URL, json=payload, headers=headers, timeout=10
                    )
                    response.raise_for_status()
                    print(
                        f"*** Mock data sent successfully (Sensor: {SENSOR_ID}, Value: {current_number}). Server response: {response.status_code} ***"
                    )
                    last_sent_number = current_number
                except requests.exceptions.RequestException as e:
                    print(f"Error sending data to server: {e}")
                except Exception as e_send:
                    print(f"An unexpected error occurred during sending: {e_send}")

        except Exception as e:
            print(f"An error occurred in the main loop: {e}")

        time.sleep(SEND_INTERVAL_SECONDS)


if __name__ == "__main__":

    main()
