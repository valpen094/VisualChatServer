set FILE=ggml-large-v3.bin

if not exist "%FILE%" (
    curl -L -o ggml-large-v3.bin https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin
)

py -3.12 -m venv mvenv
call ./mvenv/Scripts/activate
pip install -r requirements.txt
pip freeze > requirements.txt

python faster-whisper_server.py
pause