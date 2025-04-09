import time
import pytesseract
import cv2
import numpy as np
import requests
from PIL import ImageGrab, ImageOps, ImageEnhance, ImageFilter

# --- Configuration ---
# TODO: Replace with the actual URL of your Stock_Ease server endpoint
STOCK_EASE_SERVER_URL = "http://YOUR_SERVER_IP:PORT/api/weightintegration/screendata"
# TODO: Define the screen region (left_x, top_y, right_x, bottom_y) to capture
SCREEN_REGION = (100, 100, 300, 150)
# pytesseract.pytesseract.tesseract_cmd = r'/path/to/tesseract' # Uncomment if needed
CAPTURE_INTERVAL_SECONDS = 5
USE_MOCK_DATA = True # Set to False for real OCR
SENSOR_ID = "SENSOR_01" # Unique ID for this sensor/script instance

# --- Image Preprocessing Function ---
def preprocess_image(img):
    """Applies preprocessing steps to improve OCR accuracy."""
    gray_img = cv2.cvtColor(np.array(img), cv2.COLOR_BGR2GRAY)
    # Add other optional steps like thresholding, denoising, resizing here if needed
    return Image.fromarray(gray_img)

# --- Main Loop ---
def main():
    print(f"Starting OCR capture every {CAPTURE_INTERVAL_SECONDS} seconds.")
    print(f"Target URL: {STOCK_EASE_SERVER_URL}")
    print(f"Screen Region (bbox): {SCREEN_REGION}")
    if USE_MOCK_DATA:
        print("--- RUNNING IN MOCK DATA MODE ---")

    mock_counter = 100.0

    while True:
        try:
            number = None
            if USE_MOCK_DATA:
                # Generate mock data
                number = round(mock_counter + np.random.uniform(-5.0, 5.0), 2)
                print(f"Generated Mock Value: {number}")
                mock_counter += 1.0
                if mock_counter > 1000:
                    mock_counter = 100.0
            else:
                # Real OCR processing
                screenshot = ImageGrab.grab(bbox=SCREEN_REGION)
                processed_img = preprocess_image(screenshot)
                # processed_img.save("debug_capture.png") # Uncomment for debugging

                custom_config = r'--oem 3 --psm 6 -c tessedit_char_whitelist=0123456789.'
                extracted_text = pytesseract.image_to_string(processed_img, config=custom_config)
                cleaned_text = extracted_text.strip().replace(" ", "")
                print(f"Extracted Text: '{cleaned_text}'")

                try:
                    number = float(cleaned_text)
                    print(f"Detected Number: {number}")
                except ValueError:
                    if cleaned_text:
                        print(f"Could not convert extracted text '{cleaned_text}' to a number.")
                    else:
                        print("No text detected in the region.")

            # Send data if number was obtained
            if number is not None:
                try:
                    # TODO: Replace with the actual API Key configured in appsettings.json
                    api_key = "jay_nepal_this_is_a_very_secret_key"
                    headers = {'X-API-Key': api_key, 'Content-Type': 'application/json'}
                    payload = {'sensorId': SENSOR_ID, 'value': number}
                    response = requests.post(STOCK_EASE_SERVER_URL, json=payload, headers=headers, timeout=10)
                    response.raise_for_status()
                    print(f"Data sent successfully (Sensor: {SENSOR_ID}, Value: {number}). Server response: {response.status_code}")
                except requests.exceptions.RequestException as e:
                    print(f"Error sending data to server: {e}")

        except Exception as e:
            print(f"An error occurred: {e}")

        time.sleep(CAPTURE_INTERVAL_SECONDS)

if __name__ == "__main__":
    # Installation/Configuration Notes (Keep these for user reference)
    # 1. Install Python 3, Tesseract OCR (`brew install tesseract`), and Python libs (`pip3 install opencv-python pytesseract Pillow numpy requests`).
    # 2. Grant screen recording permissions if needed.
    # 3. Update STOCK_EASE_SERVER_URL, SCREEN_REGION, and api_key variable.
    # 4. Adjust image preprocessing and potentially pytesseract.pytesseract.tesseract_cmd if needed.
    main()
