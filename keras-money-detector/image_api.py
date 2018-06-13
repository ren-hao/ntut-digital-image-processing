# coding: utf-8
import io
import sys

import keras.models
import numpy as np
# from IPython.display import display
from PIL import Image
from flask import Flask, request

app = Flask(__name__)
items = ['0', '100', '1000']
model_label = ["100", "1000", "N"]
IMG_W, IMG_H = 360, 202
modelD = None
current_money = 0


def get_money(p_arr):
    global model_label
    cond = p_arr >= 0.87
    total = 0
    for i in range(0, p_arr.shape[0]):
        if cond[i]:
            total += int(model_label[i])

    return total


def load_model():
    global modelD
    if modelD is None:
        modelD = keras.models.load_model("T:\\money3.h5", compile=True)
    return modelD


def load_img(img_buffer):
    img = Image.open(img_buffer)
    img = img.resize((IMG_W, IMG_H), Image.ANTIALIAS)
    img = np.array(img, dtype=np.float32)
    o = np.ones(img.shape)
    img = img - (o * 127.5)
    img = img / 127.5
    return img


@app.route('/')
def hello_world():
    return "Hello World!"


@app.route('/money', methods=['POST'])
def upload_file():
    global current_money
    try:
        file = request.stream.read()
    except Exception as e:
        file = None
        print(e, file=sys.stderr)

    if file is None:
        return '0'

    im = load_img(io.BytesIO(file))
    p_x = np.asarray([im])
    model = load_model()
    p_arr = model.predict(p_x)
    current_money = get_money(p_arr[0])
    print(current_money)
    return str(current_money)


if __name__ == '__main__':
    m = load_model()
    tmp = [np.array(Image.new('RGB', (IMG_W, IMG_H)))]  # https://zhuanlan.zhihu.com/p/27101000
    tmp = np.asarray(tmp)
    m.predict(tmp)
    app.run(host='127.0.0.1', port=4329)
