<?xml version="1.0" encoding="UTF-8"?>
<!-- A synthetic S-125 dataset for the chart server tests: two fictitious
     aids to navigation in open water. Not real data; not for navigation. -->
<S125:Dataset xmlns:S125="http://www.iho.int/S125/1.0"
              xmlns:S100="http://www.iho.int/s100gml/5.0"
              xmlns:gml="http://www.opengis.net/gml/3.2"
              gml:id="DS_SoundChartsTests">

  <S100:DatasetIdentificationInformation>
    <S100:productIdentifier>S-125</S100:productIdentifier>
  </S100:DatasetIdentificationInformation>

  <S125:member>
    <S125:LateralBuoy gml:id="buoy1">
      <S125:geometry>
        <S100:pointProperty>
          <gml:Point gml:id="pt1">
            <gml:pos>10.0000 -30.0000</gml:pos>
          </gml:Point>
        </S100:pointProperty>
      </S125:geometry>
      <S125:categoryOfLateralMark>1</S125:categoryOfLateralMark>
    </S125:LateralBuoy>
  </S125:member>

  <S125:member>
    <S125:LateralBuoy gml:id="buoy2">
      <S125:geometry>
        <S100:pointProperty>
          <gml:Point gml:id="pt2">
            <gml:pos>10.0100 -30.0100</gml:pos>
          </gml:Point>
        </S100:pointProperty>
      </S125:geometry>
      <S125:categoryOfLateralMark>2</S125:categoryOfLateralMark>
    </S125:LateralBuoy>
  </S125:member>

</S125:Dataset>
