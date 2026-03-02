from osgeo import gdal, osr
from lazy import lazy
from tools import write_log

class GDALInterface(object):
    SEA_LEVEL = 0
    def __init__(self, tif_path):
        super(GDALInterface, self).__init__()
        self.tif_path = tif_path
        self.loadMetadata()

    def get_corner_coords(self):
        ulx, xres, xskew, uly, yskew, yres = self.geo_transform
        lrx = ulx + (self.src.RasterXSize * xres)
        lry = uly + (self.src.RasterYSize * yres)
        return {
            'TOP_LEFT': (ulx, uly),
            'TOP_RIGHT': (lrx, uly),
            'BOTTOM_LEFT': (ulx, lry),
            'BOTTOM_RIGHT': (lrx, lry),
        }

    def loadMetadata(self):
        # open the raster and its spatial reference
        self.src = gdal.Open(self.tif_path)

        if self.src is None:
            write_log(f'Could not load GDAL file "{self.tif_path}"')
            raise Exception(f'Could not load GDAL file "{self.tif_path}"')
        # spatial_reference_raster = osr.SpatialReference(self.src.GetProjection())
        spatial_reference_raster = osr.SpatialReference()
        spatial_reference_raster.ImportFromEPSG(4326)

        # get the WGS84 spatial reference
        spatial_reference = osr.SpatialReference()
        spatial_reference.ImportFromEPSG(4326)  # WGS84

        # coordinate transformation
        self.coordinate_transform = osr.CoordinateTransformation(spatial_reference, spatial_reference_raster)
        gt = self.geo_transform = self.src.GetGeoTransform()
        dev = (gt[1] * gt[5] - gt[2] * gt[4])
        self.geo_transform_inv = (gt[0], gt[5] / dev, -gt[2] / dev,
                                  gt[3], -gt[4] / dev, gt[1] / dev)

    @lazy
    def points_array(self):
        b = self.src.GetRasterBand(1)
        return b.ReadAsArray()

    def lookup(self, lat, lon, transform=False):
        try:
            if (transform):
                # get coordinate of the raster
                xgeo, ygeo, zgeo = self.coordinate_transform.TransformPoint(lon, lat, 0)

                # convert it to pixel/line on band
                u = xgeo - self.geo_transform_inv[0]
                v = ygeo - self.geo_transform_inv[3]
                xpix = int(self.geo_transform_inv[1] * u + self.geo_transform_inv[2] * v)
                ylin = int(self.geo_transform_inv[4] * u + self.geo_transform_inv[5] * v)

                # look the value up
                v = self.points_array[ylin, xpix]

                return v if v != -32768 else self.SEA_LEVEL
            else:
                # Преобразуем координаты (широта, долгота) в пиксельные координаты
                # Используем гео-трансформацию растра
                xgeo = lon
                ygeo = lat

                # Преобразуем географические координаты в пиксельные
                u = xgeo - self.geo_transform_inv[0]
                v = ygeo - self.geo_transform_inv[3]
                xpix = int(self.geo_transform_inv[1] * u + self.geo_transform_inv[2] * v)
                ylin = int(self.geo_transform_inv[4] * u + self.geo_transform_inv[5] * v)

                # Получаем значение высоты из растра
                v = self.points_array[ylin, xpix]

                # Возвращаем значение высоты или уровень моря, если значение равно -32768
                return v if v != -32768 else self.SEA_LEVEL
        except Exception as e:
            write_log(f'Error in lookup: {e}')
            return self.SEA_LEVEL

    def close(self):
        self.src = None

    def __enter__(self):
        return self

    def __exit__(self, type, value, traceback):
        self.close()