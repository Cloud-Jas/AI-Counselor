import datetime
import logging
import pathlib
import urllib.request
import numpy as np
import PIL.Image
try:
    import tflite_runtime.interpreter as tflite
except ImportError:
    import tensorflow.lite as tflite

logger = logging.getLogger(__name__)
global_predictor = None
MODEL_PATH = pathlib.Path('model.tflite')
LABELS_PATH = pathlib.Path('labels.txt')
IS_BGR = True


class Predictor:
    def __init__(self, model_path, labels_path):
        logger.debug(f"Loading model from {model_path}")
        self._interpreter = tflite.Interpreter(model_path=str(model_path))
        self._interpreter.allocate_tensors()

        input_details = self._interpreter.get_input_details()
        output_details = self._interpreter.get_output_details()
        assert len(input_details) == 1
        assert len(output_details) == 1
        self._input_index = input_details[0]['index']
        self._output_index = output_details[0]['index']

        input_size = int(input_details[0]['shape'][1])
        logger.debug(f"Model input size: {input_size}")
        self._preprocessor = Preprocessor(input_size, is_bgr=IS_BGR)

        self._labels = [label.strip() for label in labels_path.read_text().splitlines()]
        logger.debug(f"Model labels: {self._labels}")

    @property
    def labels(self):
        return self._labels

    def predict(self, image: PIL.Image.Image):
        input_array = self._preprocessor.preprocess(image)
        input_array = input_array[np.newaxis, :, :, :]

        self._interpreter.set_tensor(self._input_index, input_array)
        self._interpreter.invoke()

        outputs = self._interpreter.get_tensor(self._output_index)
        assert len(outputs) == 1
        return outputs[0].tolist()


class Preprocessor:
    def __init__(self, input_size: int, is_bgr: bool):
        self._input_size = input_size
        self._is_bgr = is_bgr

    def preprocess(self, image: PIL.Image.Image):
        image = self._update_orientation(image)
        image = self._resize_keep_aspect_ratio(image)
        image = self._crop_center(image)

        image = image.convert('RGB') if image.mode != 'RGB' else image
        np_array = np.array(image, dtype=np.float32)
        if self._is_bgr:
            np_array = np_array[:, :, (2, 1, 0)]
        return np_array

    def _update_orientation(self, image: PIL.Image.Image):
        exif_orientation_tag = 0x0112
        if hasattr(image, '_getexif'):
            exif = image._getexif()
            if exif is not None and exif_orientation_tag in exif:
                orientation = exif.get(exif_orientation_tag, 1)
                # orientation is 1 based, shift to zero based and flip/transpose based on 0-based values
                orientation -= 1
                if orientation >= 4:
                    image = image.transpose(PIL.Image.TRANSPOSE)
                if orientation in [2, 3, 6, 7]:
                    image = image.transpose(PIL.Image.FLIP_TOP_BOTTOM)
                if orientation in [1, 2, 5, 6]:
                    image = image.transpose(PIL.Image.FLIP_LEFT_RIGHT)
        return image

    def _resize_keep_aspect_ratio(self, image: PIL.Image.Image):
        width, height = image.size
        aspect_ratio = width / height
        if width < height:
            new_width = self._input_size
            new_height = round(new_width / aspect_ratio)
        else:
            new_height = self._input_size
            new_width = round(new_height * aspect_ratio)
        return image.resize((new_width, new_height), PIL.Image.BILINEAR)

    def _crop_center(self, image: PIL.Image.Image):
        width, height = image.size
        left = (width - self._input_size) // 2
        top = (height - self._input_size) // 2
        right = left + self._input_size
        bottom = top + self._input_size
        return image.crop((left, top, right, bottom))


def initialize():
    global global_predictor
    global_predictor = Predictor(MODEL_PATH, LABELS_PATH)


def predict_image(pil_image):
    assert isinstance(pil_image, PIL.Image.Image)
    global global_predictor
    assert global_predictor is not None
    outputs = global_predictor.predict(pil_image)
    predictions = [{'tagName': label, 'probability': round(p, 8), 'tagId': '', 'boundingBox': None} for label, p in zip(global_predictor.labels, outputs)]
    response = {'id': '', 'project': '', 'iteration': '', 'created': datetime.datetime.utcnow().isoformat(), 'predictions': predictions}

    return response


def predict_url(image_url):
    logger.info(f"Predicting image from {image_url}")
    with urllib.request.urlopen(image_url) as f:
        image = PIL.Image.open(f)
        return predict_image(image)
