import os
from os import listdir
from os.path import isfile, join, getsize
import json
from rtree import index
from gdal_interface import GDALInterface
from tools import write_log

class GDALTileInterface(object):
    def __init__(self, tiles_folder, summary_file, open_interfaces_size=5):
        super(GDALTileInterface, self).__init__()
        self.tiles_folder = tiles_folder
        self.summary_file = summary_file
        self.index = index.Index()
        self.cached_open_interfaces = []
        self.cached_open_interfaces_dict = {}
        self.open_interfaces_size = open_interfaces_size

    def _open_gdal_interface(self, path):
        if path in self.cached_open_interfaces_dict:
            interface = self.cached_open_interfaces_dict[path]
            self.cached_open_interfaces.remove(path)
            self.cached_open_interfaces += [path]
            return interface
        else:
            interface = GDALInterface(path)
            self.cached_open_interfaces += [path]
            self.cached_open_interfaces_dict[path] = interface

            if len(self.cached_open_interfaces) > self.open_interfaces_size:
                last_interface_path = self.cached_open_interfaces.pop(0)
                last_interface = self.cached_open_interfaces_dict[last_interface_path]
                last_interface.close()
                self.cached_open_interfaces_dict[last_interface_path] = None
                del self.cached_open_interfaces_dict[last_interface_path]

            return interface

    def _all_files(self):
        return [f for f in listdir(self.tiles_folder) if isfile(join(self.tiles_folder, f)) and f.endswith(u'.tif')]

    def has_summary_json(self):
        return os.path.exists(self.summary_file)

    def create_summary_json(self):
        all_coords = []
        for file in self._all_files():
            full_path = join(self.tiles_folder, file)
            write_log(f'Processing {full_path} ... ({getsize(full_path) / 2**20} MB)')
            i = self._open_gdal_interface(full_path)
            coords = i.get_corner_coords()

            lmin, lmax = coords['BOTTOM_RIGHT'][1], coords['TOP_RIGHT'][1]
            lngmin, lngmax = coords['TOP_LEFT'][0], coords['TOP_RIGHT'][0]
            all_coords += [
                {
                    'file': full_path,
                    'coords': (lmin, lmax, lngmin, lngmax)
                }
            ]
            write_log(f'\tDone! LAT ({lmin},{lmax}) | LNG ({lngmin},{lngmax})')

        with open(self.summary_file, 'w') as f:
            json.dump(all_coords, f)

        self.all_coords = all_coords
        self._build_index()

    def read_summary_json(self):
        with open(self.summary_file) as f:
            self.all_coords = json.load(f)
        self._build_index()

    def lookup(self, lat, lng):
        nearest = list(self.index.nearest((lat, lng), 1, objects=True))

        if not nearest:
            write_log(f'Invalid latitude/longitude: ({lat}, {lng})')
            raise Exception(f'Invalid latitude/longitude: ({lat}, {lng})')
        else:
            coords = nearest[0].object
            gdal_interface = self._open_gdal_interface(coords['file'])
            return int(gdal_interface.lookup(lat, lng, False))

    def _build_index(self):
        write_log('Building spatial index ...')
        index_id = 1
        for e in self.all_coords:
            e['index_id'] = index_id
            left, bottom, right, top = (e['coords'][0], e['coords'][2], e['coords'][1], e['coords'][3])
            self.index.insert(index_id, (left, bottom, right, top), obj=e)

    def close_all_interfaces(self):
        for interface in self.cached_open_interfaces_dict.values():
            if interface is not None:
                interface.close()
        self.cached_open_interfaces.clear()
        self.cached_open_interfaces_dict.clear()
        write_log("All GDAL interfaces closed.")
        