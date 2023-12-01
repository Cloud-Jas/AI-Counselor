FROM arm32v7/python:3.7.10-buster

  
RUN echo "deb http://archive.debian.org/debian stretch main" > /etc/apt/sources.list
RUN apt-get update
RUN pip3 install --upgrade pip 
RUN pip3 install --upgrade setuptools 
RUN pip3 install flask
RUN pip3 install wheel
RUN pip install --no-cache-dir numpy==1.16.2
RUN pip install Pillow==5.4.1
RUN pip install tflite-runtime
#RUN pip3 install https://github.com/iCorv/tflite-runtime/raw/master/tflite_runtime-2.4.0-cp37-cp37m-linux_armv7l.whl
 
COPY requirements.txt ./
RUN pip install -r requirements.txt

COPY app /app

EXPOSE 80

WORKDIR /app

CMD ["python3", "-u","app.py"]