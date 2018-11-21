from flask import Flask
from flask import request
import urlparse

app = Flask(__name__)

mesh_data = ''

@app.route('/')
def hello_world():
    return  'Hello World!'

@app.route('/upload', methods = ['POST', 'PUT'])
def save_mesh():
    global mesh_data
    mesh_data = request.data

    with open('mesh.obj','wb') as f:
        f.write(urlparse.unquote(mesh_data))

    return 'upload completed'

@app.route('/download')
def load_mesh():
    return urlparse.unquote(mesh_data)
