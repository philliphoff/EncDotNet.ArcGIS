<?xml version="1.0" encoding="UTF-8"?>
<!-- A synthetic S-125 dataset for the Object information tests: a beacon with
     a light on it, at 36.95N 76.05W, inside an area, and an information type
     the light refers to. Not real data. -->
<S125:Dataset xmlns:S125="http://www.iho.int/S125/1.0"
              xmlns:S100="http://www.iho.int/s100gml/5.0"
              xmlns:gml="http://www.opengis.net/gml/3.2"
              xmlns:xlink="http://www.w3.org/1999/xlink"
              gml:id="DS_SoundCharts_Pick">

  <S100:DatasetIdentificationInformation>
    <S100:productIdentifier>S-125</S100:productIdentifier>
  </S100:DatasetIdentificationInformation>

  <S125:imember>
    <S125:AtonStatusInformation gml:id="info1">
      <S125:changeTypes>2</S125:changeTypes>
      <S125:changeDetails>Light reported unreliable</S125:changeDetails>
    </S125:AtonStatusInformation>
  </S125:imember>

  <S125:member>
    <S125:CardinalBeacon gml:id="beacon1">
      <S125:geometry>
        <S100:pointProperty>
          <gml:Point gml:id="pt_beacon1">
            <gml:pos>36.9500 -76.0500</gml:pos>
          </gml:Point>
        </S100:pointProperty>
      </S125:geometry>
      <S125:categoryOfCardinalMark>1</S125:categoryOfCardinalMark>
    </S125:CardinalBeacon>
  </S125:member>

  <S125:member>
    <S125:LightAllAround gml:id="light1">
      <S125:geometry>
        <S100:pointProperty>
          <gml:Point gml:id="pt_light1">
            <gml:pos>36.9500 -76.0500</gml:pos>
          </gml:Point>
        </S100:pointProperty>
      </S125:geometry>
      <S125:colour>3</S125:colour>
      <S125:AtoNStatus xlink:href="#info1"/>
      <S125:parent xlink:href="#beacon1"/>
    </S125:LightAllAround>
  </S125:member>

  <S125:member>
    <S125:DataCoverage gml:id="area1">
      <S125:geometry>
        <S100:surfaceProperty>
          <gml:Surface gml:id="s_area1">
            <gml:patches>
              <gml:PolygonPatch>
                <gml:exterior>
                  <gml:LinearRing>
                    <gml:posList>36.90 -76.10 36.90 -76.00 37.00 -76.00 37.00 -76.10 36.90 -76.10</gml:posList>
                  </gml:LinearRing>
                </gml:exterior>
              </gml:PolygonPatch>
            </gml:patches>
          </gml:Surface>
        </S100:surfaceProperty>
      </S125:geometry>
    </S125:DataCoverage>
  </S125:member>

</S125:Dataset>
